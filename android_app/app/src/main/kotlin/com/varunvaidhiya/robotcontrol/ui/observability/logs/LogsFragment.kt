package com.varunvaidhiya.robotcontrol.ui.observability.logs

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import com.varunvaidhiya.robotcontrol.databinding.FragmentObsLogsBinding
import com.varunvaidhiya.robotcontrol.ui.observability.ObservabilityViewModel
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

class LogsFragment : Fragment() {

    private var _binding: FragmentObsLogsBinding? = null
    private val binding get() = _binding!!
    private val viewModel: ObservabilityViewModel by activityViewModels()
    private lateinit var adapter: LogsAdapter

    private var autoScroll  = true
    private var isLive      = false
    private var machineFilter = ""    // "" = all
    private var severityFilter = ""   // "" = all

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentObsLogsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        adapter = LogsAdapter()
        binding.recyclerLogs.layoutManager = LinearLayoutManager(requireContext()).apply {
            stackFromEnd = true
        }
        binding.recyclerLogs.adapter = adapter

        setupFilters()

        binding.btnAutoScroll.setOnClickListener {
            autoScroll = !autoScroll
            binding.btnAutoScroll.text = if (autoScroll) "AUTO-SCROLL ✓" else "AUTO-SCROLL  "
            binding.btnAutoScroll.setTextColor(
                if (autoScroll) Color.parseColor("#00E5FF") else Color.parseColor("#6100E5FF")
            )
        }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.logEntries.collectLatest { entries ->
                    adapter.submitList(entries)
                    binding.textLogCount.text = "${entries.size} entries"
                    if (autoScroll && entries.isNotEmpty()) {
                        binding.recyclerLogs.post {
                            binding.recyclerLogs.smoothScrollToPosition(0)
                        }
                    }
                }
            }
        }

        // Initial fetch
        fetchLogs()
    }

    private fun setupFilters() {
        binding.groupMachine.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (!isChecked) return@addOnButtonCheckedListener
            machineFilter = when (checkedId) {
                com.varunvaidhiya.robotcontrol.R.id.btn_machine_pi  -> "raspberry_pi"
                com.varunvaidhiya.robotcontrol.R.id.btn_machine_gpu -> "gpu_desktop"
                else -> ""
            }
            fetchLogs()
        }
        binding.groupSeverity.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (!isChecked) return@addOnButtonCheckedListener
            severityFilter = when (checkedId) {
                com.varunvaidhiya.robotcontrol.R.id.btn_sev_err  -> "ERROR"
                com.varunvaidhiya.robotcontrol.R.id.btn_sev_warn -> "WARN"
                else -> ""
            }
            fetchLogs()
        }
        binding.btnLive.setOnClickListener {
            isLive = !isLive
            val cyan = Color.parseColor("#00E5FF")
            val muted = Color.parseColor("#6100E5FF")
            binding.btnLive.setTextColor(if (isLive) cyan else muted)
            binding.btnLive.strokeColor = android.content.res.ColorStateList.valueOf(if (isLive) cyan else muted)
            if (isLive) startLive() else fetchLogs()
        }
        // Select defaults
        binding.groupMachine.check(com.varunvaidhiya.robotcontrol.R.id.btn_machine_all)
        binding.groupSeverity.check(com.varunvaidhiya.robotcontrol.R.id.btn_sev_all)
    }

    private fun buildLogQL(): String {
        val parts = mutableListOf<String>()
        if (machineFilter.isNotEmpty()) parts += "machine=\"$machineFilter\""
        if (severityFilter.isNotEmpty()) parts += "severity=\"$severityFilter\""
        return if (parts.isEmpty()) "{job=~\".+\"}" else "{${parts.joinToString(",")}}"
    }

    private fun fetchLogs() {
        val ip = viewModel.gpuIp.value
        if (ip.isBlank()) return
        viewModel.fetchLogs(ip, buildLogQL())
    }

    private fun startLive() {
        val ip = viewModel.gpuIp.value
        if (ip.isBlank()) return
        viewModel.startLiveTail(ip, buildLogQL())
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
