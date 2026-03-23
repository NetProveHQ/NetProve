package com.netprove.app.model

data class GameSession(
    val packageName: String,
    val gameName: String,
    val startTime: Long = System.currentTimeMillis(),
    var endTime: Long? = null,
    val cpuSamples: MutableList<Float> = mutableListOf(),
    val ramSamples: MutableList<Float> = mutableListOf(),
    val pingSamples: MutableList<Double> = mutableListOf(),
    val jitterSamples: MutableList<Double> = mutableListOf(),
    val packetLossSamples: MutableList<Double> = mutableListOf()
) {
    val isActive: Boolean get() = endTime == null
    val durationMs: Long get() = (endTime ?: System.currentTimeMillis()) - startTime
}
