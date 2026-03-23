package com.netprove.app.model

data class NetworkMetrics(
    val pingMs: Double = 0.0,
    val jitterMs: Double = 0.0,
    val packetLossPercent: Double = 0.0,
    val downloadMbps: Double = 0.0,
    val uploadMbps: Double = 0.0,
    val quality: NetworkQuality = NetworkQuality.Unknown
)

enum class NetworkQuality {
    Excellent,  // ping < 30, loss < 0.5%
    Good,       // ping < 60, loss < 1%
    Fair,       // ping < 100, loss < 3%
    Poor,       // ping >= 100 or loss >= 3%
    Unknown
}
