package com.varunvaidhiya.robotcontrol.ui.observability

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.models.*
import com.varunvaidhiya.robotcontrol.data.preferences.AppPreferences
import com.varunvaidhiya.robotcontrol.data.repository.ObservabilityRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject

@OptIn(ExperimentalCoroutinesApi::class)
@HiltViewModel
class ObservabilityViewModel @Inject constructor(
    private val repo: ObservabilityRepository,
    private val prefs: AppPreferences
) : ViewModel() {

    // ── Preferences ───────────────────────────────────────────────────────────

    val gpuIp      = prefs.gpuDesktopIp.stateIn(viewModelScope, SharingStarted.Eagerly, "192.168.1.100")
    val wandbApiKey = prefs.wandbApiKey.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    val wandbEntity = prefs.wandbEntity.stateIn(viewModelScope, SharingStarted.Eagerly, "")

    // ── Health ────────────────────────────────────────────────────────────────

    val health: StateFlow<RobotHealthSnapshot> = repo.healthFlow()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), RobotHealthSnapshot())

    // ── Alerts ────────────────────────────────────────────────────────────────

    val alerts: StateFlow<List<AmAlert>> = repo.alertsFlow()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    private val _silenceResult = MutableSharedFlow<String>(extraBufferCapacity = 1)
    val silenceResult: SharedFlow<String> = _silenceResult

    fun silenceAlert(ip: String, alert: AmAlert, durationHours: Int = 4) {
        viewModelScope.launch {
            val now = java.time.Instant.now()
            val end = now.plusSeconds(durationHours * 3600L)
            val req = CreateSilenceRequest(
                matchers  = listOf(AmMatcher("alertname", alert.labels["alertname"] ?: "", false)),
                startsAt  = now.toString(),
                endsAt    = end.toString(),
                createdBy = "android_app",
                comment   = "Silenced from OmniBot app for ${durationHours}h"
            )
            val id = repo.createSilence(ip, req)
            _silenceResult.tryEmit(if (id != null) "Silenced for ${durationHours}h" else "Silence failed")
        }
    }

    // ── Logs ──────────────────────────────────────────────────────────────────

    private val _logEntries = MutableStateFlow<List<LogEntry>>(emptyList())
    val logEntries: StateFlow<List<LogEntry>> = _logEntries

    private val _logsLoading = MutableStateFlow(false)
    val logsLoading: StateFlow<Boolean> = _logsLoading

    fun fetchLogs(ip: String, logql: String) {
        viewModelScope.launch {
            _logsLoading.value = true
            val entries = repo.fetchLogs(ip, logql)
            _logEntries.value = entries
            _logsLoading.value = false
        }
    }

    fun startLiveTail(ip: String, logql: String) {
        viewModelScope.launch {
            repo.tailLogs(ip, logql).collect { entry ->
                val current = _logEntries.value.toMutableList()
                current.add(0, entry)
                if (current.size > 500) current.removeAt(current.size - 1)
                _logEntries.value = current
            }
        }
    }

    // ── W&B Training ──────────────────────────────────────────────────────────

    private val _selectedProject = MutableStateFlow(WandBProject.SMOLVLA)
    val selectedProject: StateFlow<WandBProject> = _selectedProject

    private val _wandbRuns = MutableStateFlow<List<WandBRun>>(emptyList())
    val wandbRuns: StateFlow<List<WandBRun>> = _wandbRuns

    private val _wandbHistory = MutableStateFlow<List<WandBHistoryPoint>>(emptyList())
    val wandbHistory: StateFlow<List<WandBHistoryPoint>> = _wandbHistory

    private val _wandbLoading = MutableStateFlow(false)
    val wandbLoading: StateFlow<Boolean> = _wandbLoading

    private val _selectedRun = MutableStateFlow<WandBRun?>(null)
    val selectedRun: StateFlow<WandBRun?> = _selectedRun

    fun selectProject(project: WandBProject) {
        _selectedProject.value = project
        _wandbRuns.value = emptyList()
        _wandbHistory.value = emptyList()
        _selectedRun.value = null
    }

    fun loadRuns() {
        val entity = wandbEntity.value
        val key = wandbApiKey.value
        val project = selectedProject.value
        if (entity.isBlank() || key.isBlank()) return
        viewModelScope.launch {
            _wandbLoading.value = true
            _wandbRuns.value = repo.getWandBRuns(entity, project.projectId, key)
            _wandbLoading.value = false
            // Auto-select the most recent run
            _wandbRuns.value.firstOrNull()?.let { selectRun(it) }
        }
    }

    fun selectRun(run: WandBRun) {
        _selectedRun.value = run
        val entity = wandbEntity.value
        val key = wandbApiKey.value
        val project = selectedProject.value
        viewModelScope.launch {
            _wandbLoading.value = true
            _wandbHistory.value = repo.getWandBHistory(entity, project.projectId, run.id, key)
            _wandbLoading.value = false
        }
    }
}
