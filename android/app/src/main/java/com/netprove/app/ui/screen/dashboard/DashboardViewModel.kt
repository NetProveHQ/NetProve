package com.netprove.app.ui.screen.dashboard

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.netprove.app.core.CoreEngine
import com.netprove.app.core.EventBus
import com.netprove.app.model.NetworkMetrics
import com.netprove.app.model.NetworkQuality
import com.netprove.app.model.SystemMetrics
import com.netprove.app.ui.theme.*
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class DashboardViewModel @Inject constructor(
    private val eventBus: EventBus,
    private val coreEngine: CoreEngine
) : ViewModel() {

    private val _systemMetrics = MutableStateFlow(SystemMetrics())
    val systemMetrics: StateFlow<SystemMetrics> = _systemMetrics.asStateFlow()

    private val _networkMetrics = MutableStateFlow(NetworkMetrics())
    val networkMetrics: StateFlow<NetworkMetrics> = _networkMetrics.asStateFlow()

    private val _cpuHistory = MutableStateFlow<List<Double>>(emptyList())
    val cpuHistory: StateFlow<List<Double>> = _cpuHistory.asStateFlow()

    private val _ramHistory = MutableStateFlow<List<Double>>(emptyList())
    val ramHistory: StateFlow<List<Double>> = _ramHistory.asStateFlow()

    private val _pingHistory = MutableStateFlow<List<Double>>(emptyList())
    val pingHistory: StateFlow<List<Double>> = _pingHistory.asStateFlow()

    init {
        coreEngine.start()

        viewModelScope.launch {
            eventBus.systemMetrics.collect { metrics ->
                _systemMetrics.value = metrics
                _cpuHistory.value = (_cpuHistory.value + metrics.cpuUsagePercent.toDouble()).takeLast(60)
                _ramHistory.value = (_ramHistory.value + metrics.ramUsagePercent.toDouble()).takeLast(60)
            }
        }

        viewModelScope.launch {
            eventBus.networkMetrics.collect { metrics ->
                _networkMetrics.value = metrics
                _pingHistory.value = (_pingHistory.value + metrics.pingMs).takeLast(60)
            }
        }
    }

    fun refresh() {
        // Trigger a fresh measurement cycle
        coreEngine.stop()
        coreEngine.start()
    }

    override fun onCleared() {
        super.onCleared()
        coreEngine.stop()
    }
}
