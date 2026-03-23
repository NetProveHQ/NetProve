package com.netprove.app.ui.screen.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.netprove.app.core.EventBus
import com.netprove.app.engine.PerformanceReportEngine
import com.netprove.app.model.PerformanceReport
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ReportsViewModel @Inject constructor(
    private val reportEngine: PerformanceReportEngine,
    private val eventBus: EventBus
) : ViewModel() {

    private val _report = MutableStateFlow<PerformanceReport?>(null)
    val report: StateFlow<PerformanceReport?> = _report.asStateFlow()

    private val _isCollecting = MutableStateFlow(false)
    val isCollecting: StateFlow<Boolean> = _isCollecting.asStateFlow()

    private val _sampleCount = MutableStateFlow(0)
    val sampleCount: StateFlow<Int> = _sampleCount.asStateFlow()

    fun startCollecting() {
        if (_isCollecting.value) return
        _isCollecting.value = true
        _report.value = null
        _sampleCount.value = 0

        viewModelScope.launch {
            // Collect samples for 30 seconds
            val endTime = System.currentTimeMillis() + 30_000
            while (System.currentTimeMillis() < endTime && _isCollecting.value) {
                val network = eventBus.networkMetrics.replayCache.lastOrNull()
                val system = eventBus.systemMetrics.replayCache.lastOrNull()
                if (network != null && system != null) {
                    reportEngine.addSample(network, system)
                    _sampleCount.value++
                }
                kotlinx.coroutines.delay(1000)
            }
            _report.value = reportEngine.generate()
            _isCollecting.value = false
        }
    }

    fun stopCollecting() {
        _isCollecting.value = false
    }
}
