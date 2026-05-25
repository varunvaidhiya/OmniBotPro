package com.varunvaidhiya.robotcontrol.ui.ota

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.repository.RobotRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

/**
 * ViewModel for the OTA Update screen.
 *
 * Exposes OTA status StateFlows from RobotRepository and delegates
 * /ota/* service calls (check / apply_workspace / apply_models / rollback_workspace).
 */
@HiltViewModel
class OTAViewModel @Inject constructor(
    private val repository: RobotRepository
) : ViewModel() {

    val connectionState = repository.connectionState
    val otaStatus       = repository.otaStatus
    val otaProgress     = repository.otaProgress

    private val _actionResult = MutableStateFlow<String?>(null)
    val actionResult: StateFlow<String?> = _actionResult.asStateFlow()

    // ── Derived properties from the status JSON map ───────────────────────────

    val workspaceState: String
        get() = (otaStatus.value["workspace"] as? Map<*, *>)?.get("state") as? String ?: "OFFLINE"

    val modelsState: String
        get() = (otaStatus.value["models"] as? Map<*, *>)?.get("state") as? String ?: "OFFLINE"

    val currentWorkspaceVersion: String?
        get() = (otaStatus.value["workspace"] as? Map<*, *>)?.get("current") as? String

    val pendingWorkspaceVersion: String?
        get() = (otaStatus.value["workspace"] as? Map<*, *>)?.get("pending") as? String

    val backupAvailable: Boolean
        get() = (otaStatus.value["workspace"] as? Map<*, *>)?.get("backup_available") as? Boolean ?: false

    val currentModelsVersion: String?
        get() = (otaStatus.value["models"] as? Map<*, *>)?.get("current") as? String

    val pendingModelsVersion: String?
        get() = (otaStatus.value["models"] as? Map<*, *>)?.get("pending") as? String

    // ── Actions ───────────────────────────────────────────────────────────────

    fun checkForUpdate() {
        viewModelScope.launch {
            _actionResult.value = "Checking for updates…"
            repository.checkOtaUpdate { success, message ->
                _actionResult.value = if (success) message else "Check failed: $message"
            }
        }
    }

    fun applyWorkspaceUpdate() {
        viewModelScope.launch {
            _actionResult.value = "Applying workspace update…"
            repository.applyOtaWorkspace { success, message ->
                _actionResult.value = if (success) message else "Apply failed: $message"
            }
        }
    }

    fun applyModelUpdate() {
        viewModelScope.launch {
            _actionResult.value = "Applying model update…"
            repository.applyOtaModels { success, message ->
                _actionResult.value = if (success) message else "Model apply failed: $message"
            }
        }
    }

    fun rollback() {
        viewModelScope.launch {
            _actionResult.value = "Rolling back…"
            repository.rollbackOtaWorkspace { success, message ->
                _actionResult.value = if (success) message else "Rollback failed: $message"
            }
        }
    }

    fun clearActionResult() {
        _actionResult.value = null
    }
}
