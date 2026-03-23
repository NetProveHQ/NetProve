package com.netprove.app.core

import com.netprove.app.model.*
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class EventBus @Inject constructor() {

    // System metrics (replay = 1 so new collectors get latest value)
    private val _systemMetrics = MutableSharedFlow<SystemMetrics>(replay = 1)
    val systemMetrics: SharedFlow<SystemMetrics> = _systemMetrics.asSharedFlow()

    // Network metrics
    private val _networkMetrics = MutableSharedFlow<NetworkMetrics>(replay = 1)
    val networkMetrics: SharedFlow<NetworkMetrics> = _networkMetrics.asSharedFlow()

    // Game events
    private val _gameDetected = MutableSharedFlow<GameDetectedEvent>()
    val gameDetected: SharedFlow<GameDetectedEvent> = _gameDetected.asSharedFlow()

    private val _gameEnded = MutableSharedFlow<GameEndedEvent>()
    val gameEnded: SharedFlow<GameEndedEvent> = _gameEnded.asSharedFlow()

    // Lag events
    private val _lagWarning = MutableSharedFlow<LagWarningEvent>()
    val lagWarning: SharedFlow<LagWarningEvent> = _lagWarning.asSharedFlow()

    private val _lagWarningDismiss = MutableSharedFlow<Unit>()
    val lagWarningDismiss: SharedFlow<Unit> = _lagWarningDismiss.asSharedFlow()

    // Optimization events
    private val _optimizationApplied = MutableSharedFlow<OptimizationAppliedEvent>()
    val optimizationApplied: SharedFlow<OptimizationAppliedEvent> = _optimizationApplied.asSharedFlow()

    // Mode state
    private val _autoMode = MutableStateFlow(false)
    val autoMode: StateFlow<Boolean> = _autoMode.asStateFlow()

    // Publish methods
    suspend fun publishSystemMetrics(metrics: SystemMetrics) = _systemMetrics.emit(metrics)
    suspend fun publishNetworkMetrics(metrics: NetworkMetrics) = _networkMetrics.emit(metrics)
    suspend fun publishGameDetected(event: GameDetectedEvent) = _gameDetected.emit(event)
    suspend fun publishGameEnded(event: GameEndedEvent) = _gameEnded.emit(event)
    suspend fun publishLagWarning(event: LagWarningEvent) = _lagWarning.emit(event)
    suspend fun publishLagWarningDismiss() = _lagWarningDismiss.emit(Unit)
    suspend fun publishOptimization(event: OptimizationAppliedEvent) = _optimizationApplied.emit(event)
    fun setAutoMode(enabled: Boolean) { _autoMode.value = enabled }
}
