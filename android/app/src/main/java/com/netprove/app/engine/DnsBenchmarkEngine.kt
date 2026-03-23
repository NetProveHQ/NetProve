package com.netprove.app.engine

import com.netprove.app.model.DnsBenchmarkResult
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.InetSocketAddress
import java.net.Socket
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class DnsBenchmarkEngine @Inject constructor() {

    data class DnsServer(val name: String, val primary: String, val secondary: String)

    private val servers = listOf(
        DnsServer("Google", "8.8.8.8", "8.8.4.4"),
        DnsServer("Cloudflare", "1.1.1.1", "1.0.0.1"),
        DnsServer("OpenDNS", "208.67.222.222", "208.67.220.220"),
        DnsServer("Quad9", "9.9.9.9", "149.112.112.112"),
        DnsServer("AdGuard", "94.140.14.14", "94.140.15.15"),
        DnsServer("CleanBrowsing", "185.228.168.9", "185.228.169.9"),
        DnsServer("Comodo", "8.26.56.26", "8.20.247.20")
    )

    suspend fun benchmark(): List<DnsBenchmarkResult> = withContext(Dispatchers.IO) {
        servers.map { server ->
            val pings = mutableListOf<Double>()
            repeat(5) {
                val ping = tcpPing(server.primary, 53)
                if (ping >= 0) pings.add(ping)
            }
            DnsBenchmarkResult(
                name = server.name,
                primaryDns = server.primary,
                secondaryDns = server.secondary,
                avgPingMs = if (pings.isNotEmpty()) pings.average() else 999.0
            )
        }.sortedBy { it.avgPingMs }
    }

    private fun tcpPing(host: String, port: Int): Double {
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
