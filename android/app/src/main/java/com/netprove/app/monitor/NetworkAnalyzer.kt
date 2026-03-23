package com.netprove.app.monitor

import com.netprove.app.core.EventBus
import com.netprove.app.model.NetworkMetrics
import com.netprove.app.model.NetworkQuality
import kotlinx.coroutines.*
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.InetSocketAddress
import java.net.Socket
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.math.abs

@Singleton
class NetworkAnalyzer @Inject constructor(
    private val eventBus: EventBus
) {
    private var monitorJob: Job? = null
    private var previousJitter = 0.0
    private val pingHistory = mutableListOf<Double>()

    fun start(scope: CoroutineScope, intervalMs: Long = 5000L) {
        monitorJob?.cancel()
        monitorJob = scope.launch(Dispatchers.IO) {
            while (isActive) {
                try {
                    val metrics = measure()
                    eventBus.publishNetworkMetrics(metrics)
                } catch (_: CancellationException) {
                    break
                } catch (e: Exception) {
                    // Publish zero metrics on error
                    eventBus.publishNetworkMetrics(NetworkMetrics())
                }
                delay(intervalMs)
            }
        }
    }

    fun stop() {
        monitorJob?.cancel()
        monitorJob = null
    }

    private fun measure(): NetworkMetrics {
        val pings = mutableListOf<Double>()
        var lost = 0
        val totalPings = 3

        repeat(totalPings) {
            val ping = ping("8.8.8.8")
            if (ping < 0) lost++ else pings.add(ping)
        }

        val avgPing = if (pings.isNotEmpty()) pings.average() else -1.0
        val packetLoss = (lost.toDouble() / totalPings) * 100.0

        // RFC 3550 jitter calculation
        val jitter = if (pings.size >= 2) {
            var j = previousJitter
            for (i in 1 until pings.size) {
                val diff = abs(pings[i] - pings[i - 1])
                j += (diff - j) / 16.0
            }
            previousJitter = j
            j
        } else 0.0

        // Determine quality
        val quality = when {
            avgPing < 0 -> NetworkQuality.Unknown
            avgPing < 30 && packetLoss < 0.5 -> NetworkQuality.Excellent
            avgPing < 60 && packetLoss < 1.0 -> NetworkQuality.Good
            avgPing < 100 && packetLoss < 3.0 -> NetworkQuality.Fair
            else -> NetworkQuality.Poor
        }

        return NetworkMetrics(
            pingMs = if (avgPing >= 0) avgPing else 0.0,
            jitterMs = jitter,
            packetLossPercent = packetLoss,
            quality = quality
        )
    }

    private fun ping(host: String): Double {
        // Try ICMP ping via Runtime.exec first
        return try {
            val process = Runtime.getRuntime().exec("ping -c 1 -W 2 $host")
            val reader = BufferedReader(InputStreamReader(process.inputStream))
            val output = reader.readText()
            process.waitFor()

            if (process.exitValue() == 0) {
                // Parse "time=XX.X ms" from output
                val regex = Regex("time=([\\d.]+)\\s*ms")
                val match = regex.find(output)
                match?.groupValues?.get(1)?.toDoubleOrNull() ?: tcpPing(host)
            } else {
                -1.0
            }
        } catch (_: Exception) {
            // Fallback to TCP connect timing
            tcpPing(host)
        }
    }

    private fun tcpPing(host: String, port: Int = 443): Double {
        return try {
            val socket = Socket()
            val start = System.nanoTime()
            socket.connect(InetSocketAddress(host, port), 2000)
            val elapsed = (System.nanoTime() - start) / 1_000_000.0
            socket.close()
            elapsed
        } catch (_: Exception) {
            -1.0
        }
    }
}
