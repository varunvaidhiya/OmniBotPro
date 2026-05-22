package com.varunvaidhiya.robotcontrol.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.preferences.AppPreferences
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class SettingsViewModel @Inject constructor(private val preferences: AppPreferences) : ViewModel() {

    val robotIp = preferences.robotIp
    val robotPort = preferences.robotPort
    val prometheusBase = preferences.prometheusBase
    val alertManagerBase = preferences.alertManagerBase
    val lokiBase = preferences.lokiBase
    val wandbEntity = preferences.wandbEntity
    val wandbProject = preferences.wandbProject
    val wandbApiKey = preferences.wandbApiKey

    fun saveConnectionSettings(ip: String, port: Int) {
        viewModelScope.launch {
            preferences.saveConnectionSettings(ip, port)
        }
    }

    fun saveObservabilitySettings(
        prometheusBase: String,
        alertManagerBase: String,
        lokiBase: String,
        wandbEntity: String,
        wandbProject: String,
        wandbApiKey: String
    ) {
        viewModelScope.launch {
            preferences.saveObservabilitySettings(prometheusBase, alertManagerBase, lokiBase, wandbEntity, wandbProject, wandbApiKey)
        }
    }
}
