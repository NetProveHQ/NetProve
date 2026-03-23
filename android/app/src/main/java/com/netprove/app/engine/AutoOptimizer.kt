package com.netprove.app.engine

import android.content.Context
import com.netprove.app.core.EventBus
import com.netprove.app.model.NetworkMetrics
import com.netprove.app.model.OptimizationAppliedEvent
import com.netprove.app.model.SystemMetrics
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import javax.inject.Inject
import javax.inject.Singleton

data class OptimizationRecord(
    val timestamp: Long,
    val type: String,
    val description: String
)

@Singleton
class AutoOptimizer @Inject constructor(
    private val eventBus: EventBus,
    @ApplicationContext private val context: Context
) {
    private var monitorJob: Job? = null
    private val history = mutableListOf<OptimizationRecord>()
    private val lastAppliedTime = mutableMapOf<String, Long>()

    private companion object {
        const val COOLDOWN_MS = 60_000L
        const val RAM_THRESHOLD = 80f
        const val PING_THRESHOLD = 100.0
        const val PACKET_LOSS_THRESHOLD = 2.0
        const val BATTERY_TEMP_THRESHOLD = 42f
        const val CPU_THRESHOLD = 85f
    }

    fun start(scope: CoroutineScope) {
        stop()
        monitorJob = scope.launch {
            launch { collectSystemMetrics() }
            launch { collectNetworkMetrics() }
        }
    }

    fun stop() {
        monitorJob?.cancel()
        monitorJob = null
    }

    fun getHistory(): List<OptimizationRecord> = history.toList()

    private suspend fun collectSystemMetrics() {
        eventBus.systemMetrics.collect { metrics ->
            evaluateSystem(metrics)
        }
    }

    private suspend fun collectNetworkMetrics() {
        eventBus.networkMetrics.collect { metrics ->
            evaluateNetwork(metrics)
        }
    }

    private suspend fun evaluateSystem(metrics: SystemMetrics) {
        if (metrics.ramUsagePercent > RAM_THRESHOLD) {
            applyOptimization(
                type = "RAM_PRESSURE",
                description = "RAM usage at ${metrics.ramUsagePercent.toInt()}%%. " +
                        "Consider closing background apps to free memory."
            )
        }

        if (metrics.batteryTemperature > BATTERY_TEMP_THRESHOLD) {
            applyOptimization(
                type = "THERMAL_WARNING",
                description = "Battery temperature at ${metrics.batteryTemperature}°C. " +
                        "Device may throttle. Reduce workload or remove from charger."
            )
        }

        if (metrics.cpuUsagePercent > CPU_THRESHOLD) {
            applyOptimization(
                type = "CPU_HIGH",
                description = "CPU usage at ${metrics.cpuUsagePercent.toInt()}%%. " +
                        "Heavy background processes detected. Close unused apps."
            )
        }
    }

    private suspend fun evaluateNetwork(metrics: NetworkMetrics) {
        if (metrics.pingMs > PING_THRESHOLD) {
            applyOptimization(
                type = "HIGH_LATENCY",
                description = "Ping at ${metrics.pingMs.toInt()}ms. " +
                        "Suggest flushing DNS cache and resetting network connection."
            )
        }

        if (metrics.packetLossPercent > PACKET_LOSS_THRESHOLD) {
            applyOptimization(
                type = "PACKET_LOSS",
                description = "Packet loss at ${metrics.packetLossPercent}%%. " +
                        "Suggest resetting network adapter or switching to a stable connection."
            )
        }
    }

    private suspend fun applyOptimization(type: String, description: String) {
        val now = System.currentTimeMillis()
        val lastTime = lastAppliedTime[type] ?: 0L

        if (now - lastTime < COOLDOWN_MS) return

        lastAppliedTime[type] = now

        val record = OptimizationRecord(
            timestamp = now,
            type = type,
            description = description
        )
        history.add(record)

        eventBus.publishOptimization(OptimizationAppliedEvent(actionName = "$type: $description"))
    }
}
