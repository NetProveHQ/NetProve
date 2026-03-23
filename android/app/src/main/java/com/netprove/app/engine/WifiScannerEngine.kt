package com.netprove.app.engine

import android.content.Context
import android.net.ConnectivityManager
import android.net.LinkProperties
import android.net.NetworkCapabilities
import android.net.wifi.WifiManager
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.sync.Semaphore
import kotlinx.coroutines.sync.withPermit
import kotlinx.coroutines.withContext
import java.io.BufferedReader
import java.io.File
import java.io.InputStreamReader
import java.net.Inet4Address
import java.net.InetAddress
import java.net.NetworkInterface
import javax.inject.Inject
import javax.inject.Singleton

data class NetworkDevice(
    val ipAddress: String,
    val macAddress: String,
    val hostName: String,
    val vendor: String,
    val deviceType: String,
    val pingMs: Double,
)

@Singleton
class WifiScannerEngine @Inject constructor(
    @ApplicationContext private val context: Context,
) {
    companion object {
        private const val MAX_CONCURRENT = 64
        private const val PING_TIMEOUT_SECONDS = "1"
        private const val ARP_TABLE_PATH = "/proc/net/arp"

        private val MAC_VENDORS = mapOf(
            // Apple
            "00:1C:B3" to "Apple", "3C:15:C2" to "Apple", "A4:83:E7" to "Apple",
            "AC:DE:48" to "Apple", "F0:18:98" to "Apple", "14:7D:DA" to "Apple",
            "A8:5C:2C" to "Apple", "DC:A9:04" to "Apple", "78:7B:8A" to "Apple",
            "F4:5C:89" to "Apple", "B0:34:95" to "Apple", "C8:69:CD" to "Apple",
            // Samsung
            "00:21:19" to "Samsung", "A8:F2:74" to "Samsung", "C4:73:1E" to "Samsung",
            "8C:F5:A3" to "Samsung", "50:01:BB" to "Samsung", "E4:7C:F9" to "Samsung",
            "D0:22:BE" to "Samsung", "10:D5:42" to "Samsung", "BC:14:EF" to "Samsung",
            // Huawei
            "00:E0:FC" to "Huawei", "48:46:FB" to "Huawei", "AC:CF:85" to "Huawei",
            "88:53:D4" to "Huawei", "70:8C:B6" to "Huawei", "20:F1:7C" to "Huawei",
            "C8:D1:5E" to "Huawei", "04:F9:38" to "Huawei",
            // Xiaomi
            "28:6C:07" to "Xiaomi", "64:CC:2E" to "Xiaomi", "9C:99:A0" to "Xiaomi",
            "74:23:44" to "Xiaomi", "F8:A4:5F" to "Xiaomi", "50:64:2B" to "Xiaomi",
            "AC:C1:EE" to "Xiaomi", "78:11:DC" to "Xiaomi",
            // Google / Nest
            "F4:F5:D8" to "Google", "54:60:09" to "Google", "3C:5A:B4" to "Google",
            "A4:77:33" to "Google", "18:D6:C7" to "Google",
            // Intel
            "00:1B:21" to "Intel", "3C:97:0E" to "Intel", "00:1E:64" to "Intel",
            "AC:72:89" to "Intel", "8C:8D:28" to "Intel",
            // TP-Link
            "50:C7:BF" to "TP-Link", "C0:25:E9" to "TP-Link", "14:CC:20" to "TP-Link",
            "54:C8:0F" to "TP-Link", "EC:08:6B" to "TP-Link",
            // ASUS
            "2C:4D:54" to "ASUS", "AC:22:0B" to "ASUS",
            "1C:87:2C" to "ASUS", "04:D9:F5" to "ASUS",
            // Netgear
            "00:1E:2A" to "Netgear", "A0:21:B7" to "Netgear", "C4:04:15" to "Netgear",
            "B0:B9:8A" to "Netgear", "84:1B:5E" to "Netgear",
            // D-Link
            "00:1C:F0" to "D-Link", "B8:A3:86" to "D-Link", "1C:7E:E5" to "D-Link",
            "C8:D3:A3" to "D-Link", "28:10:7B" to "D-Link",
            // Sony
            "00:1A:80" to "Sony", "04:5D:4B" to "Sony", "AC:9B:0A" to "Sony",
            "78:84:3C" to "Sony",
            // LG
            "00:1C:62" to "LG", "A8:23:FE" to "LG", "C4:36:6C" to "LG",
            "CC:2D:8C" to "LG", "10:68:3F" to "LG",
            // Microsoft / Xbox
            "00:50:F2" to "Microsoft", "28:18:78" to "Microsoft", "7C:1E:52" to "Microsoft",
            "DC:B4:C4" to "Microsoft",
            // Amazon
            "40:B4:CD" to "Amazon", "74:C2:46" to "Amazon", "FC:65:DE" to "Amazon",
            "A0:02:DC" to "Amazon", "44:65:0D" to "Amazon",
            // Dell
            "00:14:22" to "Dell", "18:03:73" to "Dell", "F8:BC:12" to "Dell",
            "B0:83:FE" to "Dell",
            // HP
            "00:1A:4B" to "HP", "3C:D9:2B" to "HP", "94:57:A5" to "HP",
            "EC:B1:D7" to "HP",
            // Lenovo
            "28:D2:44" to "Lenovo", "54:EE:75" to "Lenovo", "E8:2A:44" to "Lenovo",
            "98:FA:9B" to "Lenovo",
            // Realtek
            "00:E0:4C" to "Realtek", "52:54:00" to "Realtek", "00:0C:E7" to "Realtek",
            // OnePlus / Oppo / Realme
            "94:65:2D" to "OnePlus", "C0:EE:FB" to "OnePlus",
            "A4:77:58" to "Oppo", "3C:77:E6" to "Oppo", "E8:BB:A8" to "Oppo",
            "C8:F7:33" to "Realme",
            // Motorola
            "00:08:0E" to "Motorola", "A4:70:D6" to "Motorola", "60:BE:B5" to "Motorola",
            // Nokia / HMD
            "00:1A:DC" to "Nokia", "A0:4E:04" to "Nokia",
            // Cisco / Linksys
            "00:1A:A1" to "Cisco", "00:22:CE" to "Cisco", "00:0C:41" to "Cisco",
            "20:AA:4B" to "Linksys", "C0:56:27" to "Linksys",
            // Roku
            "D8:31:34" to "Roku", "B0:A7:37" to "Roku",
            // Sonos
            "B8:E9:37" to "Sonos", "00:0E:58" to "Sonos",
            // Raspberry Pi Foundation
            "B8:27:EB" to "Raspberry Pi", "DC:A6:32" to "Raspberry Pi",
            "E4:5F:01" to "Raspberry Pi",
            // Ubiquiti
            "00:15:6D" to "Ubiquiti", "68:72:51" to "Ubiquiti", "F0:9F:C2" to "Ubiquiti",
            // Espressif (IoT / ESP32)
            "24:0A:C4" to "Espressif", "30:AE:A4" to "Espressif",
            // Nintendo
            "00:1F:32" to "Nintendo", "58:BD:A3" to "Nintendo", "E8:4E:CE" to "Nintendo",
            // Vivo
            "E4:54:E8" to "Vivo", "98:14:A0" to "Vivo",
            // ZTE
            "34:4B:50" to "ZTE", "54:22:F8" to "ZTE",
            // Arris (cable modems / routers)
            "00:1D:CE" to "Arris", "20:3D:66" to "Arris",
        )

        private val VENDOR_DEVICE_TYPES = mapOf(
            "Apple" to "\uD83D\uDCBB",       // laptop
            "Samsung" to "\uD83D\uDCF1",     // mobile phone
            "Huawei" to "\uD83D\uDCF1",
            "Xiaomi" to "\uD83D\uDCF1",
            "OnePlus" to "\uD83D\uDCF1",
            "Oppo" to "\uD83D\uDCF1",
            "Realme" to "\uD83D\uDCF1",
            "Vivo" to "\uD83D\uDCF1",
            "Motorola" to "\uD83D\uDCF1",
            "Nokia" to "\uD83D\uDCF1",
            "Google" to "\uD83D\uDCF1",
            "Amazon" to "\uD83D\uDCE6",      // package (Echo/Fire)
            "Intel" to "\uD83D\uDCBB",       // laptop
            "Realtek" to "\uD83D\uDCBB",
            "Dell" to "\uD83D\uDCBB",
            "HP" to "\uD83D\uDCBB",
            "Lenovo" to "\uD83D\uDCBB",
            "Microsoft" to "\uD83D\uDDA5\uFE0F", // desktop computer
            "Sony" to "\uD83C\uDFAE",        // game controller
            "Nintendo" to "\uD83C\uDFAE",
            "TP-Link" to "\uD83D\uDCE1",     // satellite antenna (router)
            "ASUS" to "\uD83D\uDCE1",
            "Netgear" to "\uD83D\uDCE1",
            "D-Link" to "\uD83D\uDCE1",
            "Cisco" to "\uD83D\uDCE1",
            "Linksys" to "\uD83D\uDCE1",
            "Ubiquiti" to "\uD83D\uDCE1",
            "Arris" to "\uD83D\uDCE1",
            "ZTE" to "\uD83D\uDCE1",
            "Roku" to "\uD83D\uDCFA",        // television
            "Sonos" to "\uD83D\uDD0A",       // speaker
            "Raspberry Pi" to "\uD83E\uDD16", // robot
            "Espressif" to "\uD83E\uDD16",
            "LG" to "\uD83D\uDCFA",          // television
        )

        private const val DEFAULT_DEVICE_TYPE = "\uD83D\uDCBB" // laptop (fallback)
    }

    /**
     * Scans the local WiFi network for connected devices.
     * Pings all 254 IPs in the subnet concurrently, reads the ARP table,
     * resolves hostnames, and returns devices sorted with gateway first then by IP.
     */
    suspend fun scan(): List<NetworkDevice> = withContext(Dispatchers.IO) {
        val subnetPrefix = getSubnetPrefix() ?: return@withContext emptyList()
        val gatewayIp = getGatewayAddress()
        val semaphore = Semaphore(MAX_CONCURRENT)

        // Ping all 254 hosts concurrently to populate the ARP table
        val devices = coroutineScope {
            (1..254).map { host ->
                async(Dispatchers.IO) {
                    semaphore.withPermit {
                        val ip = "$subnetPrefix.$host"
                        pingAndDiscover(ip)
                    }
                }
            }.awaitAll().filterNotNull()
        }

        // Sort: gateway first, then by IP octets numerically
        devices.sortedWith(compareBy<NetworkDevice> { it.ipAddress != gatewayIp }
            .thenBy { ipToSortKey(it.ipAddress) })
    }

    /**
     * Pings a single IP via ICMP shell command. If the host responds,
     * gathers MAC, hostname, vendor, and device type information.
     */
    private fun pingAndDiscover(ip: String): NetworkDevice? {
        val pingMs = measurePing(ip) ?: return null

        val mac = getMacFromArp(ip) ?: "00:00:00:00:00:00"
        val hostName = resolveHostName(ip)
        val vendor = lookupVendor(mac)
        val deviceType = VENDOR_DEVICE_TYPES[vendor] ?: DEFAULT_DEVICE_TYPE

        return NetworkDevice(
            ipAddress = ip,
            macAddress = mac,
            hostName = hostName,
            vendor = vendor,
            deviceType = deviceType,
            pingMs = pingMs,
        )
    }

    /**
     * Executes an ICMP ping via Runtime.exec and parses the round-trip time.
     * Returns latency in milliseconds or null if unreachable.
     */
    private fun measurePing(ip: String): Double? {
        return try {
            val process = Runtime.getRuntime().exec(
                arrayOf("ping", "-c", "1", "-W", PING_TIMEOUT_SECONDS, ip)
            )
            val reader = BufferedReader(InputStreamReader(process.inputStream))
            val output = reader.readText()
            reader.close()

            val exited = process.waitFor()
            process.destroy()

            if (exited == 0) {
                // Parse "time=X.Y ms" from ping output
                val timeRegex = "time=(\\d+\\.?\\d*)".toRegex()
                val match = timeRegex.find(output)
                match?.groupValues?.get(1)?.toDoubleOrNull() ?: 0.0
            } else {
                null
            }
        } catch (_: Exception) {
            null
        }
    }

    /**
     * Reads the MAC address for a given IP from /proc/net/arp.
     */
    private fun getMacFromArp(ip: String): String? {
        return try {
            val file = File(ARP_TABLE_PATH)
            if (!file.exists()) return null
            file.bufferedReader().useLines { lines ->
                lines.drop(1) // skip header
                    .map { it.trim().split("\\s+".toRegex()) }
                    .firstOrNull { parts ->
                        parts.size >= 4
                                && parts[0] == ip
                                && parts[3] != "00:00:00:00:00:00"
                                && parts[3].contains(":")
                    }
                    ?.get(3)
                    ?.uppercase()
            }
        } catch (_: Exception) {
            null
        }
    }

    /**
     * Attempts reverse DNS lookup. Returns hostname or empty string if unresolvable.
     */
    private fun resolveHostName(ip: String): String {
        return try {
            val address = InetAddress.getByName(ip)
            val resolved = address.canonicalHostName
            if (resolved != ip) resolved else ""
        } catch (_: Exception) {
            ""
        }
    }

    /**
     * Looks up vendor from MAC prefix (first 3 octets / OUI).
     */
    private fun lookupVendor(mac: String): String {
        val prefix = mac.uppercase().take(8) // "XX:XX:XX"
        return MAC_VENDORS[prefix] ?: "Unknown"
    }

    /**
     * Gets the local WiFi subnet prefix (e.g. "192.168.1").
     * Tries WifiManager int IP format first, then falls back to NetworkInterface enumeration.
     */
    @Suppress("DEPRECATION")
    private fun getSubnetPrefix(): String? {
        // Try WifiManager (handles the int IP format)
        try {
            val wifiManager = context.applicationContext
                .getSystemService(Context.WIFI_SERVICE) as WifiManager
            val wifiInfo = wifiManager.connectionInfo
            val ipInt = wifiInfo.ipAddress
            if (ipInt != 0) {
                val a = ipInt and 0xFF
                val b = (ipInt shr 8) and 0xFF
                val c = (ipInt shr 16) and 0xFF
                return "$a.$b.$c"
            }
        } catch (_: Exception) {
            // Fall through
        }

        // Try ConnectivityManager + LinkProperties (modern Android)
        try {
            val cm = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
            val network = cm.activeNetwork
            if (network != null) {
                val caps = cm.getNetworkCapabilities(network)
                if (caps != null && caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)) {
                    val linkProps: LinkProperties? = cm.getLinkProperties(network)
                    linkProps?.linkAddresses?.forEach { linkAddr ->
                        val addr = linkAddr.address
                        if (addr is Inet4Address && !addr.isLoopbackAddress) {
                            return addr.hostAddress?.substringBeforeLast(".")
                        }
                    }
                }
            }
        } catch (_: Exception) {
            // Fall through
        }

        // Fallback: enumerate network interfaces
        try {
            val interfaces = NetworkInterface.getNetworkInterfaces() ?: return null
            for (iface in interfaces) {
                if (iface.isLoopback || !iface.isUp) continue
                if (!iface.name.startsWith("wlan")) continue
                for (addr in iface.inetAddresses) {
                    if (addr is Inet4Address && !addr.isLoopbackAddress) {
                        return addr.hostAddress?.substringBeforeLast(".")
                    }
                }
            }
        } catch (_: Exception) {
            // No suitable interface found
        }
        return null
    }

    /**
     * Returns the default gateway IP address by inspecting LinkProperties routes
     * or parsing /proc/net/route.
     */
    private fun getGatewayAddress(): String? {
        // Try ConnectivityManager
        try {
            val cm = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
            val network = cm.activeNetwork ?: return null
            val linkProps = cm.getLinkProperties(network) ?: return null
            for (route in linkProps.routes) {
                if (route.isDefaultRoute && route.gateway is Inet4Address) {
                    return route.gateway?.hostAddress
                }
            }
        } catch (_: Exception) {
            // Fall through
        }

        // Fallback: parse /proc/net/route (gateway hex in little-endian)
        try {
            File("/proc/net/route").bufferedReader().useLines { lines ->
                lines.drop(1).forEach { line ->
                    val parts = line.trim().split("\\s+".toRegex())
                    if (parts.size >= 3 && parts[1] == "00000000") {
                        val hex = parts[2]
                        if (hex.length == 8) {
                            val a = hex.substring(6, 8).toInt(16)
                            val b = hex.substring(4, 6).toInt(16)
                            val c = hex.substring(2, 4).toInt(16)
                            val d = hex.substring(0, 2).toInt(16)
                            return "$a.$b.$c.$d"
                        }
                    }
                }
            }
        } catch (_: Exception) {
            // Could not parse routing table
        }
        return null
    }

    /**
     * Converts an IP address string to a numeric key for natural sorting.
     */
    private fun ipToSortKey(ip: String): Long {
        val parts = ip.split(".")
        if (parts.size != 4) return 0L
        return parts.fold(0L) { acc, octet ->
            (acc shl 8) + (octet.toIntOrNull() ?: 0)
        }
    }
}
