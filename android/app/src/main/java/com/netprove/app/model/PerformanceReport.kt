package com.netprove.app.model

data class PerformanceReport(
    val id: Long = 0,
    val timestamp: Long = System.currentTimeMillis(),
    val score: Int,           // 0-100
    val rating: String,       // Excellent, Good, Fair, Poor, Critical
    val stars: Int,            // 1-5
    val avgPingMs: Double,
    val avgJitterMs: Double,
    val avgPacketLoss: Double,
    val avgCpu: Float,
    val avgRam: Float,
    val lagSpikeCount: Int,
    val suggestions: List<String>
)
