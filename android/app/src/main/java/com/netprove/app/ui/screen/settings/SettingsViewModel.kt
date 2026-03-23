package com.netprove.app.ui.screen.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.netprove.app.data.AppSettings
import com.netprove.app.data.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val repo: SettingsRepository
) : ViewModel() {

    val settings: StateFlow<AppSettings> = repo.settings
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), AppSettings())

    fun setDarkTheme(enabled: Boolean) {
        viewModelScope.launch { repo.setDarkTheme(enabled) }
    }

    fun setLanguage(code: String) {
        viewModelScope.launch { repo.setLanguage(code) }
    }

    fun setAutoMode(enabled: Boolean) {
        viewModelScope.launch { repo.setAutoMode(enabled) }
    }

    fun setNotifications(enabled: Boolean) {
        viewModelScope.launch { repo.setNotifications(enabled) }
    }
}
