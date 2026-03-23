package com.netprove.app.model

data class SpeedTestProgress(
    val phase: SpeedTestPhase,
    val progressPercent: Double,
    val currentMbps: Double
)

enum class SpeedTestPhase {
    Idle, Ping, Download, Upload, Complete
}

data class SpeedTestResult(
    val pingMs: Double,
    val downloadMbps: Double,
    val uploadMbps: Double,
    val timestamp: Long = System.currentTimeMillis()
)

data class DnsBenchmarkResult(
    val name: String,
    val primaryDns: String,
    val secondaryDns: String,
    val avgPingMs: Double,
    val isApplied: Boolean = false
)
