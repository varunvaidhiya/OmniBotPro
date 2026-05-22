package com.varunvaidhiya.robotcontrol.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.preferences.AppPreferences
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class SettingsViewModel @Inject constructor(private val preferences: AppPreferences) : ViewModel() {

    val robotIp    = preferences.robotIp
    val robotPort  = preferences.robotPort
    val gpuIp      = preferences.gpuDesktopIp
    val wandbKey   = preferences.wandbApiKey
    val wandbEntity= preferences.wandbEntity

    fun saveConnectionSettings(ip: String, port: Int) {
        viewModelScope.launch { preferences.saveConnectionSettings(ip, port) }
    }

    fun saveObservabilitySettings(gpuIp: String, wandbKey: String, wandbEntity: String) {
        viewModelScope.launch { preferences.saveObservabilitySettings(gpuIp, wandbKey, wandbEntity) }
    }
}
