package com.varunvaidhiya.robotcontrol.ui.ota

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.databinding.FragmentOtaBinding
import com.varunvaidhiya.robotcontrol.network.ROSBridgeManager
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

@AndroidEntryPoint
class OTAFragment : BaseFragment<FragmentOtaBinding>() {

    private val viewModel: OTAViewModel by viewModels()

    override fun inflateBinding(
        inflater: LayoutInflater,
        container: ViewGroup?
    ): FragmentOtaBinding = FragmentOtaBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupButtons()
        setupObservers()
    }

    // ── Button wiring ─────────────────────────────────────────────────────────

    private fun setupButtons() {
        binding.btnCheckUpdate.setOnClickListener { viewModel.checkForUpdate() }
        binding.btnApplyWorkspace.setOnClickListener { viewModel.applyWorkspaceUpdate() }
        binding.btnRollback.setOnClickListener { viewModel.rollback() }
        binding.btnApplyModels.setOnClickListener { viewModel.applyModelUpdate() }
    }

    // ── Observers ─────────────────────────────────────────────────────────────

    private fun setupObservers() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {

                // Connection state → status dot colour
                launch {
                    viewModel.connectionState.collectLatest { state ->
                        val dotRes = when (state) {
                            ROSBridgeManager.ConnectionState.CONNECTED  -> R.drawable.shape_circle_green
                            ROSBridgeManager.ConnectionState.CONNECTING -> R.drawable.shape_circle_orange
                            else -> R.drawable.shape_circle_red
                        }
                        binding.otaWsStatusDot.setBackgroundResource(dotRes)
                        binding.btnCheckUpdate.isEnabled =
                            state == ROSBridgeManager.ConnectionState.CONNECTED
                    }
                }

                // OTA status JSON → workspace card
                launch {
                    viewModel.otaStatus.collectLatest { _ ->
                        updateWorkspaceCard()
                        updateModelsCard()
                    }
                }

                // Download/install progress
                launch {
                    viewModel.otaProgress.collectLatest { pct ->
                        val wsActive = viewModel.workspaceState in listOf(
                            "DOWNLOADING", "VERIFYING", "INSTALLING"
                        )
                        val modelActive = viewModel.modelsState in listOf(
                            "DOWNLOADING", "VERIFYING"
                        )
                        if (wsActive) {
                            binding.otaWsProgressBar.progress = pct
                            binding.textWsProgressLabel.text = "$pct%"
                        }
                        if (modelActive) {
                            binding.otaModelProgressBar.progress = pct
                            binding.textModelProgressLabel.text = "$pct%"
                        }
                    }
                }

                // Action result messages
                launch {
                    viewModel.actionResult.collectLatest { result ->
                        if (result != null) {
                            binding.textActionResult.text = result
                            binding.textActionResult.visibility = View.VISIBLE
                        } else {
                            binding.textActionResult.visibility = View.GONE
                        }
                    }
                }
            }
        }
    }

    // ── Card update helpers ───────────────────────────────────────────────────

    private fun updateWorkspaceCard() {
        val state = viewModel.workspaceState
        binding.textWsState.text = "STATE: $state"

        val currentVer = viewModel.currentWorkspaceVersion
        binding.textWsCurrentVersion.text = "CURRENT: ${currentVer ?: "—"}"

        val pendingVer = viewModel.pendingWorkspaceVersion
        if (pendingVer != null) {
            binding.textWsPendingVersion.text = "PENDING: $pendingVer"
            binding.textWsPendingVersion.visibility = View.VISIBLE
        } else {
            binding.textWsPendingVersion.visibility = View.GONE
        }

        val isActive = state in listOf("DOWNLOADING", "VERIFYING", "INSTALLING", "RESTARTING")
        binding.otaWsProgressBar.visibility = if (isActive) View.VISIBLE else View.GONE
        binding.textWsProgressLabel.visibility = if (isActive) View.VISIBLE else View.GONE

        val isIdle = state == "IDLE" || state == "UPDATE_AVAILABLE" || state == "ERROR"
        binding.btnApplyWorkspace.isEnabled = isIdle && (pendingVer != null || state == "UPDATE_AVAILABLE")
        binding.btnRollback.isEnabled = isIdle && viewModel.backupAvailable
    }

    private fun updateModelsCard() {
        val state = viewModel.modelsState
        val currentVer = viewModel.currentModelsVersion
        binding.textModelCurrentVersion.text = "CURRENT: ${currentVer ?: "—"}"

        val pendingVer = viewModel.pendingModelsVersion
        if (pendingVer != null) {
            binding.textModelPendingVersion.text = "PENDING: $pendingVer"
            binding.textModelPendingVersion.visibility = View.VISIBLE
        } else {
            binding.textModelPendingVersion.visibility = View.GONE
        }

        val isActive = state in listOf("DOWNLOADING", "VERIFYING")
        binding.otaModelProgressBar.visibility = if (isActive) View.VISIBLE else View.GONE
        binding.textModelProgressLabel.visibility = if (isActive) View.VISIBLE else View.GONE

        val isIdle = state == "IDLE" || state == "UPDATE_AVAILABLE" || state == "ERROR"
        binding.btnApplyModels.isEnabled = isIdle && (pendingVer != null || state == "UPDATE_AVAILABLE")
    }
}
