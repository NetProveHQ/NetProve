package com.netprove.app.model

data class SystemMetrics(
    val cpuUsagePercent: Float = 0f,
    val ramUsagePercent: Float = 0f,
    val ramUsedMb: Long = 0,
    val ramTotalMb: Long = 0,
    val batteryPercent: Int = 100,
    val batteryTemperature: Float = 0f,
    val isCharging: Boolean = false
)
