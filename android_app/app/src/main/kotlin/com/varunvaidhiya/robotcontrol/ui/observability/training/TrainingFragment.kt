package com.varunvaidhiya.robotcontrol.ui.observability.training

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.LinearLayout
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.data.models.WandBHistoryPoint
import com.varunvaidhiya.robotcontrol.data.models.WandBProject
import com.varunvaidhiya.robotcontrol.data.models.WandBRun
import com.varunvaidhiya.robotcontrol.databinding.FragmentObsTrainingBinding
import com.varunvaidhiya.robotcontrol.ui.observability.ObservabilityViewModel
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

class TrainingFragment : Fragment() {

    private var _binding: FragmentObsTrainingBinding? = null
    private val binding get() = _binding!!
    private val viewModel: ObservabilityViewModel by activityViewModels()
    private lateinit var runsAdapter: WandBRunAdapter

    private val chartColors = listOf(
        Color.parseColor("#00E5FF"), Color.parseColor("#00FF7F"),
        Color.parseColor("#FFD700"), Color.parseColor("#FF6B6B")
    )

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentObsTrainingBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        setupRecyclerView()
        setupChart()
        setupProjectSelector()

        binding.btnRefreshRuns.setOnClickListener { viewModel.loadRuns() }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch { observeRuns() }
                launch { observeHistory() }
                launch { observeLoading() }
                launch { observeSelectedRun() }
                launch { observeCredentials() }
            }
        }

        // Initial load
        if (viewModel.wandbApiKey.value.isNotBlank()) viewModel.loadRuns()
    }

    private fun setupRecyclerView() {
        runsAdapter = WandBRunAdapter { run -> viewModel.selectRun(run) }
        binding.recyclerRuns.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerRuns.adapter = runsAdapter
        binding.recyclerRuns.isNestedScrollingEnabled = false
    }

    private fun setupChart() {
        binding.chartMetrics.apply {
            setBackgroundColor(Color.TRANSPARENT)
            description.isEnabled = false
            legend.isEnabled = false
            setTouchEnabled(true)
            isDragEnabled = true
            setScaleEnabled(false)
            setDrawGridBackground(false)

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                textColor = Color.parseColor("#6100E5FF")
                textSize  = 7f
                setDrawGridLines(true)
                gridColor = Color.parseColor("#1400E5FF")
                axisLineColor = Color.parseColor("#3800E5FF")
            }
            axisLeft.apply {
                textColor = Color.parseColor("#6100E5FF")
                textSize  = 7f
                setDrawGridLines(true)
                gridColor = Color.parseColor("#1400E5FF")
                axisLineColor = Color.parseColor("#3800E5FF")
            }
            axisRight.isEnabled = false
        }
    }

    private fun setupProjectSelector() {
        binding.groupProject.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (!isChecked) return@addOnButtonCheckedListener
            val project = when (checkedId) {
                R.id.btn_proj_rl_nav -> WandBProject.RL_NAV
                R.id.btn_proj_rl_arm -> WandBProject.RL_ARM
                else                 -> WandBProject.SMOLVLA
            }
            viewModel.selectProject(project)
            if (viewModel.wandbApiKey.value.isNotBlank()) viewModel.loadRuns()
        }
        binding.groupProject.check(R.id.btn_proj_smolvla)
    }

    private suspend fun observeCredentials() {
        viewModel.wandbApiKey.collectLatest { key ->
            binding.layoutNoCreds.visibility = if (key.isBlank()) View.VISIBLE else View.GONE
        }
    }

    private suspend fun observeRuns() {
        viewModel.wandbRuns.collectLatest { runs ->
            runsAdapter.submitList(runs)
        }
    }

    private suspend fun observeSelectedRun() {
        viewModel.selectedRun.collectLatest { run ->
            if (run == null) {
                binding.cardRunDetail.visibility = View.GONE
                return@collectLatest
            }
            binding.cardRunDetail.visibility = View.VISIBLE
            runsAdapter.setSelected(run.id)
            populateRunDetail(run)
        }
    }

    private fun populateRunDetail(run: WandBRun) {
        binding.textRunTitle.text = run.name
        val stateColor = stateColor(run.state)
        binding.textRunStateDetail.text  = run.state.uppercase()
        binding.textRunStateDetail.setTextColor(stateColor)

        // Summary metrics grid
        binding.layoutSummaryMetrics.removeAllViews()
        run.summary.entries
            .filter { it.value is Double || it.value is Int || it.value is Long }
            .take(6)
            .forEach { (key, value) ->
                val row = LayoutInflater.from(requireContext())
                    .inflate(android.R.layout.simple_list_item_2, binding.layoutSummaryMetrics, false)
                (row.findViewById<TextView>(android.R.id.text1))?.apply {
                    text = key.substringAfterLast("/")
                    textSize = 7f
                    setTextColor(Color.parseColor("#6100E5FF"))
                    typeface = android.graphics.Typeface.MONOSPACE
                }
                (row.findViewById<TextView>(android.R.id.text2))?.apply {
                    val d = when (value) {
                        is Double -> value
                        is Int    -> value.toDouble()
                        is Long   -> value.toDouble()
                        else      -> 0.0
                    }
                    text = "%.4f".format(d)
                    textSize = 10f
                    setTextColor(Color.parseColor("#00E5FF"))
                    typeface = android.graphics.Typeface.MONOSPACE
                }
                binding.layoutSummaryMetrics.addView(row)
            }
    }

    private suspend fun observeHistory() {
        viewModel.wandbHistory.collectLatest { history ->
            if (history.isEmpty()) {
                binding.cardChart.visibility = View.GONE
                return@collectLatest
            }
            binding.cardChart.visibility = View.VISIBLE
            renderChart(history)
        }
    }

    private fun renderChart(history: List<WandBHistoryPoint>) {
        val project  = viewModel.selectedProject.value
        val keyPrefs = project.chartKeys

        // Find which keys actually exist in the data
        val availableKeys = keyPrefs.filter { key ->
            history.any { it.metrics.containsKey(key) }
        }.ifEmpty {
            // Fallback: pick first 3 numeric keys
            history.firstOrNull()?.metrics?.keys?.take(3)?.toList() ?: return
        }

        val dataSets = availableKeys.mapIndexed { idx, key ->
            val entries = history.mapNotNull { pt ->
                val v = pt.metrics[key] ?: return@mapNotNull null
                Entry(pt.step.toFloat(), v.toFloat())
            }
            LineDataSet(entries, key.substringAfterLast("/")).apply {
                color = chartColors.getOrElse(idx) { chartColors[0] }
                lineWidth = 1.5f
                setDrawCircles(false)
                setDrawValues(false)
                mode = LineDataSet.Mode.CUBIC_BEZIER
            }
        }

        binding.chartMetrics.data = LineData(dataSets)
        binding.chartMetrics.invalidate()

        // Legend
        binding.layoutChartLegend.removeAllViews()
        dataSets.forEachIndexed { idx, ds ->
            val tv = TextView(requireContext()).apply {
                text = "■ ${availableKeys.getOrElse(idx) { "" }.substringAfterLast("/")}"
                textSize = 7f
                setTextColor(chartColors.getOrElse(idx) { chartColors[0] })
                typeface = android.graphics.Typeface.MONOSPACE
                setPadding(0, 0, 16, 0)
            }
            binding.layoutChartLegend.addView(tv)
        }
    }

    private suspend fun observeLoading() {
        viewModel.wandbLoading.collectLatest { loading ->
            binding.textLoading.visibility = if (loading) View.VISIBLE else View.GONE
        }
    }

    private fun stateColor(state: String) = when (state.lowercase()) {
        "running"  -> Color.parseColor("#00FF7F")
        "finished" -> Color.parseColor("#00E5FF")
        else       -> Color.parseColor("#FF4444")
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
