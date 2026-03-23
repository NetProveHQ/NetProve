package com.netprove.app.ui.screen.dns

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.netprove.app.engine.DnsBenchmarkEngine
import com.netprove.app.model.DnsBenchmarkResult
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class DnsBenchmarkViewModel @Inject constructor(
    private val engine: DnsBenchmarkEngine
) : ViewModel() {

    private val _results = MutableStateFlow<List<DnsBenchmarkResult>>(emptyList())
    val results: StateFlow<List<DnsBenchmarkResult>> = _results.asStateFlow()

    private val _isRunning = MutableStateFlow(false)
    val isRunning: StateFlow<Boolean> = _isRunning.asStateFlow()

    fun runBenchmark() {
        if (_isRunning.value) return
        _isRunning.value = true
        _results.value = emptyList()

        viewModelScope.launch {
            try {
                _results.value = engine.benchmark()
            } catch (_: Exception) {
                // Keep empty results on failure
            } finally {
                _isRunning.value = false
            }
        }
    }
}
