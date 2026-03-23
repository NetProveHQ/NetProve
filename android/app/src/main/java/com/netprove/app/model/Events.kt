package com.netprove.app.model

data class GameDetectedEvent(
    val packageName: String,
    val gameName: String
)

data class GameEndedEvent(
    val packageName: String,
    val gameName: String
)

data class LagWarningEvent(
    val detail: String,
    val severity: LagSeverity = LagSeverity.Medium,
    val confidence: Double = 0.0
)

data class OptimizationAppliedEvent(
    val actionName: String
)

enum class LagSeverity {
    None, Low, Medium, High, Critical
}

enum class LagCause {
    CpuBottleneck,
    RamPressure,
    NetworkLatencySpike,
    PacketLoss,
    UnstableConnection,
    BackgroundInterference,
    ThermalThrottling
}

data class LagAnalysisResult(
    val summary: String,
    val severity: LagSeverity,
    val cpuPercent: Float,
    val ramPercent: Float,
    val pingMs: Double,
    val jitterMs: Double,
    val packetLossPercent: Double,
    val batteryTemp: Float,
    val causes: List<LagCauseDetail>,
    val recommendations: List<String>
)

data class LagCauseDetail(
    val cause: LagCause,
    val confidence: Double,
    val description: String
)

data class LagPrediction(
    val predictedLag: Boolean,
    val reason: String,
    val confidence: Double,
    val estimatedSecondsUntilLag: Int
)
