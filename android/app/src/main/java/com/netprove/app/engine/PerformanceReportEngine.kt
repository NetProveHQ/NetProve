package com.netprove.app.engine

import com.netprove.app.model.PerformanceReport
import com.netprove.app.model.NetworkMetrics
import com.netprove.app.model.SystemMetrics
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class PerformanceReportEngine @Inject constructor() {

    private val networkSamples = mutableListOf<NetworkMetrics>()
    private val systemSamples = mutableListOf<SystemMetrics>()
    private var lagSpikeCount = 0

    fun addSample(network: NetworkMetrics, system: SystemMetrics) {
        networkSamples.add(network)
        systemSamples.add(system)

        if (network.pingMs > 150 || network.packetLossPercent > 3 || network.jitterMs > 30) {
            lagSpikeCount++
        }
    }

    fun generate(): PerformanceReport? {
        if (networkSamples.isEmpty()) return null

        val avgPing = networkSamples.map { it.pingMs }.average()
        val avgJitter = networkSamples.map { it.jitterMs }.average()
        val avgPacketLoss = networkSamples.map { it.packetLossPercent }.average()
        val avgCpu = systemSamples.map { it.cpuUsagePercent }.average().toFloat()
        val avgRam = systemSamples.map { it.ramUsagePercent }.average().toFloat()

        // Scoring (port from Windows PerformanceReportEngine)
        var score = 100
        score -= (avgPacketLoss * 10).toInt().coerceAtMost(30)
        score -= ((avgPing - 30).coerceAtLeast(0.0) / 3).toInt().coerceAtMost(25)
        score -= ((avgJitter - 5).coerceAtLeast(0.0) / 2).toInt().coerceAtMost(20)
        score -= (lagSpikeCount * 2).coerceAtMost(15)
        score -= ((avgCpu - 70).coerceAtLeast(0f) / 3).toInt().coerceAtMost(10)
        score = score.coerceIn(0, 100)

        val (rating, stars) = when {
            score >= 90 -> "Mükemmel" to 5
            score >= 75 -> "İyi" to 4
            score >= 55 -> "Orta" to 3
            score >= 35 -> "Kötü" to 2
            else -> "Kritik" to 1
        }

        val suggestions = buildList {
            if (avgPing > 80) add("Ping yüksek — daha yakın bir DNS sunucusu deneyin")
            if (avgPacketLoss > 2) add("Paket kaybı var — Wi-Fi sinyalinizi kontrol edin")
            if (avgJitter > 20) add("Jitter yüksek — arka plan indirmelerini durdurun")
            if (avgCpu > 80) add("CPU yüksek — arka plan uygulamalarını kapatın")
            if (avgRam > 85) add("RAM baskısı — uygulamaları temizleyin")
            if (lagSpikeCount > 5) add("Çok sayıda gecikme sıçraması — bağlantınız dengesiz")
            if (isEmpty()) add("Her şey yolunda görünüyor!")
        }

        val report = PerformanceReport(
            score = score,
            rating = rating,
            stars = stars,
            avgPingMs = avgPing,
            avgJitterMs = avgJitter,
            avgPacketLoss = avgPacketLoss,
            avgCpu = avgCpu,
            avgRam = avgRam,
            lagSpikeCount = lagSpikeCount,
            suggestions = suggestions
        )

        // Reset for next report
        networkSamples.clear()
        systemSamples.clear()
        lagSpikeCount = 0

        return report
    }
}
