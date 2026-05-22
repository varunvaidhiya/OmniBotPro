package com.varunvaidhiya.robotcontrol.ui.observability.health

import android.content.res.ColorStateList
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
import com.varunvaidhiya.robotcontrol.data.models.RobotHealthSnapshot
import com.varunvaidhiya.robotcontrol.databinding.FragmentObsHealthBinding
import com.varunvaidhiya.robotcontrol.ui.observability.ObservabilityViewModel
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class HealthFragment : Fragment() {

    private var _binding: FragmentObsHealthBinding? = null
    private val binding get() = _binding!!
    private val viewModel: ObservabilityViewModel by activityViewModels()

    private val colorOk   = Color.parseColor("#00FF7F")
    private val colorWarn = Color.parseColor("#FFD700")
    private val colorErr  = Color.parseColor("#FF4444")
    private val colorCyan = Color.parseColor("#00E5FF")
    private val timeFmt   = SimpleDateFormat("HH:mm:ss", Locale.US)

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentObsHealthBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.health.collectLatest { snap -> applySnapshot(snap) }
            }
        }
    }

    private fun applySnapshot(s: RobotHealthSnapshot) {
        binding.textUpdateTime.text = "LAST UPDATE: ${timeFmt.format(Date(s.timestamp))}"

        // Targets summary
        val up = s.targets.count { it.health == "up" }
        binding.textTargetsStatus.text = "TARGETS: $up/${s.targets.size}"
        binding.textTargetsStatus.setTextColor(if (up == s.targets.size) colorOk else colorWarn)

        // Alert banner
        val totalAlerts = s.firingCritical + s.firingWarning
        binding.cardAlertBanner.visibility = if (totalAlerts > 0) View.VISIBLE else View.GONE
        if (totalAlerts > 0) {
            binding.textAlertSummary.text = buildString {
                if (s.firingCritical > 0) append("${s.firingCritical} CRITICAL")
                if (s.firingWarning  > 0) {
                    if (s.firingCritical > 0) append(" · ")
                    append("${s.firingWarning} WARNING")
                }
                append(" — TAP ALERTS TAB")
            }
        }

        // E-Stop
        val estopColor = if (s.estopActive) colorErr else colorOk
        binding.accentEstop.setBackgroundColor(estopColor)
        binding.textEstopState.text = if (s.estopActive) "ACTIVE" else "CLEAR"
        binding.textEstopState.setTextColor(estopColor)

        // Control mode
        binding.textControlMode.text = s.controlMode.uppercase()

        // Pi resources
        applyBar(binding.barPiCpu,  binding.textPiCpu,  s.piCpuPct,  "%")
        applyBar(binding.barPiRam,  binding.textPiRam,  s.piRamPct,  "%")
        applyBar(binding.barPiDisk, binding.textPiDisk, s.piDiskPct, "%")

        // GPU resources
        binding.textGpuTemp.text = "%.0f°C".format(s.gpuTempC)
        binding.textGpuTemp.setTextColor(metricColor(s.gpuTempC, 70.0, 85.0))
        applyBarColored(binding.barGpuUtil,  binding.textGpuUtil,  s.gpuUtilPct,  "%",  Color.parseColor("#80FF80"))
        applyBarColored(binding.barGpuVram,  binding.textGpuVram,  s.gpuVramPct,  "%",  Color.parseColor("#80FF80"))

        // Node timing — driver
        binding.textDriverP50.text = "%.1fms".format(s.driverP50ms)
        binding.textDriverP95.text = "%.1fms".format(s.driverP95ms)
        binding.textDriverMax.text = "%.1fms".format(s.driverMaxMs)
        val driverColor = metricColor(s.driverP95ms, 55.0, 100.0)
        binding.textDriverP50.setTextColor(driverColor)
        binding.textDriverP95.setTextColor(driverColor)
        binding.textDriverMax.setTextColor(driverColor)

        binding.textVlaMs.text   = if (s.vlaInferenceMs > 0) "%.0fms".format(s.vlaInferenceMs) else "--ms"
        binding.textVlaMs.setTextColor(metricColor(s.vlaInferenceMs, 2000.0, 5000.0))
        binding.textRlNavMs.text = if (s.rlNavMs > 0) "%.1fms".format(s.rlNavMs) else "--ms"
        binding.textRlNavMs.setTextColor(metricColor(s.rlNavMs, 55.0, 100.0))
        binding.textRlArmMs.text = if (s.rlArmMs > 0) "%.1fms".format(s.rlArmMs) else "--ms"
        binding.textRlArmMs.setTextColor(metricColor(s.rlArmMs, 55.0, 100.0))

        // Robot velocity
        binding.textRobotVx.text    = "%+.3f".format(s.robotVx)
        binding.textRobotVy.text    = "%+.3f".format(s.robotVy)
        binding.textRobotOmega.text = "%+.3f".format(s.robotOmega)

        // Mission rate
        if (s.missionSuccessRate >= 0) {
            val pct = s.missionSuccessRate * 100.0
            binding.textMissionRate.text = "%.0f%%".format(pct)
            binding.textMissionRate.setTextColor(if (pct >= 70) colorOk else if (pct >= 40) colorWarn else colorErr)
        } else {
            binding.textMissionRate.text = "N/A"
            binding.textMissionRate.setTextColor(Color.parseColor("#6100E5FF"))
        }
    }

    private fun applyBar(bar: android.widget.ProgressBar, label: android.widget.TextView, value: Double, suffix: String) {
        val color = metricColor(value, 80.0, 95.0)
        bar.progress = value.coerceIn(0.0, 100.0).toInt()
        bar.progressTintList = ColorStateList.valueOf(color)
        label.text = "%.0f$suffix".format(value)
        label.setTextColor(color)
    }

    private fun applyBarColored(bar: android.widget.ProgressBar, label: android.widget.TextView, value: Double, suffix: String, baseColor: Int) {
        val color = if (value >= 95) colorErr else if (value >= 85) colorWarn else baseColor
        bar.progress = value.coerceIn(0.0, 100.0).toInt()
        bar.progressTintList = ColorStateList.valueOf(color)
        label.text = "%.0f$suffix".format(value)
        label.setTextColor(color)
    }

    private fun metricColor(value: Double, warnThreshold: Double, errThreshold: Double): Int = when {
        value <= 0      -> colorCyan
        value >= errThreshold  -> colorErr
        value >= warnThreshold -> colorWarn
        else -> colorOk
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
