package com.netprove.app.core

import com.netprove.app.engine.GameDetector
import com.netprove.app.engine.LagPredictionEngine
import com.netprove.app.monitor.NetworkAnalyzer
import com.netprove.app.monitor.SystemMonitor
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class CoreEngine @Inject constructor(
    val eventBus: EventBus,
    private val networkAnalyzer: NetworkAnalyzer,
    private val systemMonitor: SystemMonitor,
    private val gameDetector: GameDetector,
    private val lagPredictionEngine: LagPredictionEngine
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private var running = false

    fun start() {
        if (running) return
        running = true
        systemMonitor.start(scope, 3000L)
        networkAnalyzer.start(scope, 5000L)
        gameDetector.start(scope, 10000L)
        lagPredictionEngine.start(scope)
    }

    fun stop() {
        running = false
        systemMonitor.stop()
        networkAnalyzer.stop()
        gameDetector.stop()
        lagPredictionEngine.stop()
    }
}
