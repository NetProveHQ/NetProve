package com.netprove.app.ui.screen.speedtest

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.netprove.app.engine.SpeedTestEngine
import com.netprove.app.model.SpeedTestPhase
import com.netprove.app.model.SpeedTestResult
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class SpeedTestViewModel @Inject constructor(
    private val speedTestEngine: SpeedTestEngine
) : ViewModel() {

    private val _phase = MutableStateFlow(SpeedTestPhase.Idle)
    val phase: StateFlow<SpeedTestPhase> = _phase.asStateFlow()

    private val _progress = MutableStateFlow(0.0)
    val progress: StateFlow<Double> = _progress.asStateFlow()

    private val _currentSpeed = MutableStateFlow(0.0)
    val currentSpeed: StateFlow<Double> = _currentSpeed.asStateFlow()

    private val _result = MutableStateFlow<SpeedTestResult?>(null)
    val result: StateFlow<SpeedTestResult?> = _result.asStateFlow()

    private val _isRunning = MutableStateFlow(false)
    val isRunning: StateFlow<Boolean> = _isRunning.asStateFlow()

    fun runTest() {
        if (_isRunning.value) return
        _isRunning.value = true
        _result.value = null

        viewModelScope.launch {
            speedTestEngine.run().collect { p ->
                _phase.value = p.phase
                _progress.value = p.progressPercent
                _currentSpeed.value = p.currentMbps

                if (p.phase == SpeedTestPhase.Complete) {
                    _result.value = speedTestEngine.lastResult
                    _isRunning.value = false
                }
            }
        }
    }
}
