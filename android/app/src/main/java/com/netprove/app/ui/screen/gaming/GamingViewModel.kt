package com.netprove.app.ui.screen.gaming

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.netprove.app.core.EventBus
import com.netprove.app.engine.GameDetector
import com.netprove.app.engine.LagPredictionEngine
import com.netprove.app.model.LagPrediction
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class GamingViewModel @Inject constructor(
    private val eventBus: EventBus,
    private val gameDetector: GameDetector,
    private val lagPrediction: LagPredictionEngine
) : ViewModel() {

    private val _activeGame = MutableStateFlow<String?>(null)
    val activeGame: StateFlow<String?> = _activeGame.asStateFlow()

    private val _lagWarning = MutableStateFlow<String?>(null)
    val lagWarning: StateFlow<String?> = _lagWarning.asStateFlow()

    private val _prediction = MutableStateFlow<LagPrediction?>(null)
    val prediction: StateFlow<LagPrediction?> = _prediction.asStateFlow()

    init {
        viewModelScope.launch {
            eventBus.gameDetected.collect { event ->
                _activeGame.value = event.gameName
            }
        }
        viewModelScope.launch {
            eventBus.gameEnded.collect {
                _activeGame.value = null
            }
        }
        viewModelScope.launch {
            eventBus.lagWarning.collect { event ->
                _lagWarning.value = event.detail
                _prediction.value = lagPrediction.latest
            }
        }
        viewModelScope.launch {
            eventBus.lagWarningDismiss.collect {
                _lagWarning.value = null
            }
        }
    }
}
