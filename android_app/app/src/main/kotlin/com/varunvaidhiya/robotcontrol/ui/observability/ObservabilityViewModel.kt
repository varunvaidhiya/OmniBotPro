package com.varunvaidhiya.robotcontrol.ui.observability

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.models.observability.*
import com.varunvaidhiya.robotcontrol.data.preferences.AppPreferences
import com.varunvaidhiya.robotcontrol.data.repository.ObservabilityRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class ObservabilityViewModel @Inject constructor(
    private val repo: ObservabilityRepository,
    private val prefs: AppPreferences
) : ViewModel() {

    // Config from preferences
    val prometheusBase: Flow<String> = prefs.prometheusBase
    val alertManagerBase: Flow<String> = prefs.alertManagerBase
    val lokiBase: Flow<String> = prefs.lokiBase
    val wandbEntity: Flow<String> = prefs.wandbEntity
    val wandbProject: Flow<String> = prefs.wandbProject
    val wandbApiKey: Flow<String> = prefs.wandbApiKey

    // Robot health
    private val _robotHealth = MutableStateFlow<RobotHealthData?>(null)
    val robotHealth: StateFlow<RobotHealthData?> = _robotHealth.asStateFlow()

    // Alerts
    private val _alerts = MutableStateFlow<List<AlertManagerAlert>>(emptyList())
    val alerts: StateFlow<List<AlertManagerAlert>> = _alerts.asStateFlow()

    // Logs
    private val _logs = MutableStateFlow<List<LogEntry>>(emptyList())
    val logs: StateFlow<List<LogEntry>> = _logs.asStateFlow()

    // W&B runs
    private val _wandbRuns = MutableStateFlow<List<WandBRun>>(emptyList())
    val wandbRuns: StateFlow<List<WandBRun>> = _wandbRuns.asStateFlow()

    // Error states
    private val _healthError = MutableStateFlow<String?>(null)
    val healthError: StateFlow<String?> = _healthError.asStateFlow()

    private val _logsError = MutableStateFlow<String?>(null)
    val logsError: StateFlow<String?> = _logsError.asStateFlow()

    private val _alertsError = MutableStateFlow<String?>(null)
    val alertsError: StateFlow<String?> = _alertsError.asStateFlow()

    private val _wandbError = MutableStateFlow<String?>(null)
    val wandbError: StateFlow<String?> = _wandbError.asStateFlow()

    // Loading
    private val _isLoadingHealth = MutableStateFlow(false)
    val isLoadingHealth: StateFlow<Boolean> = _isLoadingHealth.asStateFlow()

    private var pollingJob: Job? = null

    fun startPolling() {
        if (pollingJob?.isActive == true) return
        pollingJob = viewModelScope.launch {
            while (true) {
                refreshAll()
                delay(5_000L)
            }
        }
    }

    fun stopPolling() {
        pollingJob?.cancel()
        pollingJob = null
    }

    fun refreshAll() {
        viewModelScope.launch {
            val pBase = prometheusBase.first()
            val aBase = alertManagerBase.first()
            val lBase = lokiBase.first()
            val entity = wandbEntity.first()
            val project = wandbProject.first()
            val key = wandbApiKey.first()

            launch {
                _isLoadingHealth.value = true
                repo.fetchRobotHealth(pBase)
                    .onSuccess { _robotHealth.value = it; _healthError.value = null }
                    .onFailure { _healthError.value = "Prometheus: ${it.message}" }
                _isLoadingHealth.value = false
            }
            launch {
                repo.fetchAlerts(aBase)
                    .onSuccess { _alerts.value = it; _alertsError.value = null }
                    .onFailure { _alertsError.value = "AlertManager: ${it.message}" }
            }
            launch {
                repo.fetchLogs(lBase)
                    .onSuccess { _logs.value = it; _logsError.value = null }
                    .onFailure { _logsError.value = "Loki: ${it.message}" }
            }
            launch {
                repo.fetchWandBRuns(entity, project, key)
                    .onSuccess { _wandbRuns.value = it; _wandbError.value = null }
                    .onFailure { _wandbError.value = "W&B: ${it.message}" }
            }
        }
    }

    override fun onCleared() {
        super.onCleared()
        stopPolling()
    }
}
