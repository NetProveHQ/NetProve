package com.netprove.app.engine

import com.netprove.app.core.EventBus
import com.netprove.app.model.LagPrediction
import com.netprove.app.model.LagSeverity
import com.netprove.app.model.SystemMetrics
import com.netprove.app.model.NetworkMetrics
import kotlinx.coroutines.*
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.math.abs

@Singleton
class LagPredictionEngine @Inject constructor(
    private val eventBus: EventBus
) {
    private var monitorJob: Job? = null

    private val cpuHistory = mutableListOf<Double>()
    private val ramHistory = mutableListOf<Double>()
    private val pingHistory = mutableListOf<Double>()
    private val jitterHistory = mutableListOf<Double>()
    private val packetLossHistory = mutableListOf<Double>()
    private val batteryTempHistory = mutableListOf<Double>()

    var latest: LagPrediction? = null
        private set

    private val windowSize = 10

    fun start(scope: CoroutineScope) {
        monitorJob?.cancel()

        // Collect metrics
        monitorJob = scope.launch {
            launch {
                eventBus.systemMetrics.collect { m ->
                    addSample(cpuHistory, m.cpuUsagePercent.toDouble())
                    addSample(ramHistory, m.ramUsagePercent.toDouble())
                    addSample(batteryTempHistory, m.batteryTemperature.toDouble())
                }
            }
            launch {
                eventBus.networkMetrics.collect { m ->
                    addSample(pingHistory, m.pingMs)
                    addSample(jitterHistory, m.jitterMs)
                    addSample(packetLossHistory, m.packetLossPercent)
                }
            }
            launch {
                while (isActive) {
                    delay(10_000) // Predict every 10 seconds
                    latest = predict()
                    val pred = latest
                    if (pred != null && pred.predictedLag) {
                        eventBus.publishLagWarning(
                            com.netprove.app.model.LagWarningEvent(
                                detail = pred.reason,
                                severity = when {
                                    pred.confidence >= 80 -> LagSeverity.High
                                    pred.confidence >= 60 -> LagSeverity.Medium
                                    else -> LagSeverity.Low
                                },
                                confidence = pred.confidence
                            )
                        )
                    }
                }
            }
        }
    }

    fun stop() {
        monitorJob?.cancel()
        monitorJob = null
    }

    private fun predict(): LagPrediction {
        val reasons = mutableListOf<String>()
        var maxConfidence = 0.0
        var totalEta = 30

        // CPU trend
        if (cpuHistory.size >= windowSize) {
            val slope = computeSlope(cpuHistory.takeLast(windowSize))
            val latest = cpuHistory.last()
            if (slope > 3.0 && latest > 60) {
                val conf = ((slope * 10 + (latest - 60)).coerceAtMost(95.0))
                reasons.add("CPU yükselme eğilimi (%.1f%%, +%.1f/s)".format(latest, slope))
                maxConfidence = maxOf(maxConfidence, conf)
                totalEta = minOf(totalEta, ((90 - latest) / slope).toInt().coerceAtLeast(5))
            }
        }

        // RAM trend
        if (ramHistory.size >= windowSize) {
            val slope = computeSlope(ramHistory.takeLast(windowSize))
            val latest = ramHistory.last()
            if (slope > 1.5 && latest > 75) {
                val conf = ((slope * 15 + (latest - 75)).coerceAtMost(95.0))
                reasons.add("RAM baskısı artıyor (%.1f%%, +%.1f/s)".format(latest, slope))
                maxConfidence = maxOf(maxConfidence, conf)
                totalEta = minOf(totalEta, ((95 - latest) / slope).toInt().coerceAtLeast(5))
            }
        }

        // Ping trend
        if (pingHistory.size >= windowSize) {
            val slope = computeSlope(pingHistory.takeLast(windowSize))
            val latest = pingHistory.last()
            if (slope > 2.0 && latest > 50) {
                val conf = ((slope * 8 + (latest - 50) * 0.5).coerceAtMost(95.0))
                reasons.add("Ping yükseliyor (%.0f ms, +%.1f/s)".format(latest, slope))
                maxConfidence = maxOf(maxConfidence, conf)
                totalEta = minOf(totalEta, ((150 - latest) / slope).toInt().coerceAtLeast(5))
            }
        }

        // Jitter spike
        if (jitterHistory.size >= 5) {
            val recent = jitterHistory.takeLast(5)
            val avg = recent.average()
            val max = recent.max()
            if (max > 30 && max > avg * 2.5) {
                reasons.add("Jitter sıçraması (%.1f ms)".format(max))
                maxConfidence = maxOf(maxConfidence, 70.0)
                totalEta = minOf(totalEta, 10)
            }
        }

        // Packet loss trend
        if (packetLossHistory.size >= 3) {
            val recentAvg = packetLossHistory.takeLast(3).average()
            if (recentAvg >= 1.0) {
                val conf = (recentAvg * 20).coerceAtMost(95.0)
                reasons.add("Paket kaybı (%.1f%%)".format(recentAvg))
                maxConfidence = maxOf(maxConfidence, conf)
                totalEta = minOf(totalEta, 15)
            }
        }

        // Battery thermal throttling (mobile-specific)
        if (batteryTempHistory.size >= 5) {
            val slope = computeSlope(batteryTempHistory.takeLast(5))
            val latest = batteryTempHistory.last()
            if (latest > 40 && slope > 0.5) {
                reasons.add("Termal kısıtlama riski (%.1f°C)".format(latest))
                maxConfidence = maxOf(maxConfidence, 65.0)
                totalEta = minOf(totalEta, 20)
            }
        }

        return if (reasons.isNotEmpty()) {
            LagPrediction(
                predictedLag = true,
                reason = reasons.joinToString("; "),
                confidence = maxConfidence,
                estimatedSecondsUntilLag = totalEta
            )
        } else {
            LagPrediction(
                predictedLag = false,
                reason = "Tüm metrikler stabil",
                confidence = 0.0,
                estimatedSecondsUntilLag = 0
            )
        }
    }

    private fun computeSlope(values: List<Double>): Double {
        val n = values.size
        if (n < 2) return 0.0
        val xMean = (n - 1) / 2.0
        val yMean = values.average()
        var num = 0.0
        var den = 0.0
        for (i in values.indices) {
            val dx = i - xMean
            num += dx * (values[i] - yMean)
            den += dx * dx
        }
        return if (den > 0) num / den else 0.0
    }

    private fun addSample(history: MutableList<Double>, value: Double) {
        history.add(value)
        if (history.size > 60) history.removeAt(0)
    }
}
