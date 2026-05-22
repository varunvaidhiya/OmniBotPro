package com.varunvaidhiya.robotcontrol.ui.observability

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import com.varunvaidhiya.robotcontrol.databinding.FragmentObservabilityBinding
import com.varunvaidhiya.robotcontrol.data.models.observability.*
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import com.varunvaidhiya.robotcontrol.ui.observability.adapters.AlertsAdapter
import com.varunvaidhiya.robotcontrol.ui.observability.adapters.LogsAdapter
import com.varunvaidhiya.robotcontrol.ui.observability.adapters.WandBRunsAdapter
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import java.util.Locale

@AndroidEntryPoint
class ObservabilityFragment : BaseFragment<FragmentObservabilityBinding>() {

    private val viewModel: ObservabilityViewModel by viewModels()
    private lateinit var alertsAdapter: AlertsAdapter
    private lateinit var logsAdapter: LogsAdapter
    private lateinit var wandbAdapter: WandBRunsAdapter

    private val colorOk    = Color.parseColor("#00FF7F")
    private val colorWarn  = Color.parseColor("#FFD700")
    private val colorErr   = Color.parseColor("#FF4444")
    private val colorCyan  = Color.parseColor("#00E5FF")

    override fun inflateBinding(inflater: LayoutInflater, container: ViewGroup?) =
        FragmentObservabilityBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupAdapters()
        setupCollapseToggles()
        setupRefreshButton()
        observeData()
        viewModel.startPolling()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        viewModel.stopPolling()
    }

    private fun setupAdapters() {
        alertsAdapter = AlertsAdapter()
        binding.recyclerAlerts.apply {
            layoutManager = LinearLayoutManager(requireContext())
            adapter = alertsAdapter
            isNestedScrollingEnabled = false
        }
        logsAdapter = LogsAdapter()
        binding.recyclerLogs.apply {
            layoutManager = LinearLayoutManager(requireContext())
            adapter = logsAdapter
            isNestedScrollingEnabled = false
        }
        wandbAdapter = WandBRunsAdapter()
        binding.recyclerWandb.apply {
            layoutManager = LinearLayoutManager(requireContext())
            adapter = wandbAdapter
            isNestedScrollingEnabled = false
        }
    }

    private fun setupCollapseToggles() {
        binding.headerHealth.setOnClickListener { toggle(binding.contentHealth, binding.chevronHealth) }
        binding.headerAi.setOnClickListener { toggle(binding.contentAi, binding.chevronAi) }
        binding.headerAlerts.setOnClickListener { toggle(binding.contentAlerts, binding.chevronAlerts) }
        binding.headerLogs.setOnClickListener { toggle(binding.contentLogs, binding.chevronLogs) }
    }

    private fun toggle(content: View, chevron: TextView) {
        if (content.visibility == View.VISIBLE) {
            content.visibility = View.GONE
            chevron.text = "▶"
        } else {
            content.visibility = View.VISIBLE
            chevron.text = "▼"
        }
    }

    private fun setupRefreshButton() {
        binding.btnRefresh.setOnClickListener { viewModel.refreshAll() }
    }

    private fun observeData() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch { viewModel.robotHealth.collectLatest { it?.let(::bindRobotHealth) } }
                launch { viewModel.alerts.collectLatest { bindAlerts(it) } }
                launch { viewModel.logs.collectLatest { bindLogs(it) } }
                launch { viewModel.wandbRuns.collectLatest { bindWandB(it) } }
                launch {
                    viewModel.healthError.collectLatest { err ->
                        binding.textHealthError.text = err ?: ""
                        binding.textHealthError.visibility = if (err != null) View.VISIBLE else View.GONE
                    }
                }
                launch {
                    viewModel.alertsError.collectLatest { err ->
                        binding.textAlertsError.text = err ?: ""
                        binding.textAlertsError.visibility = if (err != null) View.VISIBLE else View.GONE
                    }
                }
                launch {
                    viewModel.logsError.collectLatest { err ->
                        binding.textLogsError.text = err ?: ""
                        binding.textLogsError.visibility = if (err != null) View.VISIBLE else View.GONE
                    }
                }
                launch {
                    viewModel.wandbError.collectLatest { err ->
                        binding.textWandbError.text = err ?: ""
                        binding.textWandbError.visibility = if (err != null) View.VISIBLE else View.GONE
                    }
                }
                launch {
                    viewModel.isLoadingHealth.collectLatest { loading ->
                        binding.btnRefresh.text = if (loading) "REFRESHING…" else "↺  REFRESH"
                    }
                }
            }
        }
    }

    private fun bindRobotHealth(h: RobotHealthData) {
        // E-stop
        val estopColor = if (h.estopActive) colorErr else colorOk
        binding.valEstop.text = if (h.estopActive) "ACTIVE ⚠" else "CLEAR"
        binding.valEstop.setTextColor(estopColor)
        binding.dotEstop.setBackgroundResource(
            if (h.estopActive) com.varunvaidhiya.robotcontrol.R.drawable.shape_circle_red
            else com.varunvaidhiya.robotcontrol.R.drawable.shape_circle_green
        )
        // Control mode
        binding.valControlMode.text = h.controlMode.uppercase()
        // Velocity
        binding.valVx.text = String.format(Locale.US, "%+.3f", h.vx)
        binding.valVy.text = String.format(Locale.US, "%+.3f", h.vy)
        binding.valOmega.text = String.format(Locale.US, "%+.3f", h.omega)
        // Driver cycle
        val cycleColor = when {
            h.driverCycleP95Ms > 100.0 -> colorErr
            h.driverCycleP95Ms > 55.0  -> colorWarn
            else -> colorOk
        }
        binding.valCycleP95.text = String.format(Locale.US, "%.1fms", h.driverCycleP95Ms)
        binding.valCycleP95.setTextColor(cycleColor)
        // Missions
        binding.valMissionsTotal.text = h.missionsTotal.toInt().toString()
        val successRate = if (h.missionsTotal > 0) h.missionsDoneOk / h.missionsTotal * 100.0 else 0.0
        binding.valMissionSuccess.text = String.format(Locale.US, "%.0f%%", successRate)
        binding.valMissionSuccess.setTextColor(if (successRate >= 70.0) colorOk else colorWarn)
        // VLA
        val vlaColor = when {
            h.vlaInferenceMs > 5000.0 -> colorErr
            h.vlaInferenceMs > 2000.0 -> colorWarn
            else -> colorOk
        }
        binding.valVlaMs.text = if (h.vlaInferenceMs > 0) String.format(Locale.US, "%.0fms", h.vlaInferenceMs) else "--"
        binding.valVlaMs.setTextColor(vlaColor)
    }

    private fun bindAlerts(alerts: List<AlertManagerAlert>) {
        val count = alerts.size
        binding.badgeAlertsCount.text = if (count > 0) " [$count FIRING]" else " [OK]"
        binding.badgeAlertsCount.setTextColor(if (count > 0) colorErr else colorOk)
        alertsAdapter.submitList(alerts)
        binding.textNoAlerts.visibility = if (count == 0) View.VISIBLE else View.GONE
    }

    private fun bindLogs(logs: List<LogEntry>) {
        logsAdapter.submitList(logs.take(30))
        binding.textNoLogs.visibility = if (logs.isEmpty()) View.VISIBLE else View.GONE
    }

    private fun bindWandB(runs: List<WandBRun>) {
        wandbAdapter.submitList(runs)
        binding.textNoWandb.visibility = if (runs.isEmpty()) View.VISIBLE else View.GONE
    }
}
