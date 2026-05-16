package com.varunvaidhiya.robotcontrol.ui.home

import android.content.res.ColorStateList
import android.graphics.Color
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ProgressBar
import android.widget.TextView
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.databinding.FragmentHomeBinding
import com.varunvaidhiya.robotcontrol.network.ROSBridgeManager
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import com.varunvaidhiya.robotcontrol.ui.dashboard.DashboardViewModel
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import kotlin.math.roundToInt
import kotlin.random.Random

@AndroidEntryPoint
class HomeFragment : BaseFragment<FragmentHomeBinding>() {

    private val viewModel: DashboardViewModel by viewModels()
    private val handler = Handler(Looper.getMainLooper())

    // Simulated metric state (real data would come from /diagnostics)
    private var battMain = 78.2f
    private var battArm = 91.5f
    private var cores = floatArrayOf(42f, 58f, 28f, 45f)
    private var rpiTemp = 52.3f
    private var rpiRam = 1.82f
    private var wsGpu = 68f
    private var wsVram = 4.2f
    private var wsCpu = 45f
    private var wsRam = 22.4f
    private var netUp = 1.2f
    private var netDown = 0.8f
    private var vlaLoad = 62f

    private val colorOk   = Color.parseColor("#00FF7F")
    private val colorWarn = Color.parseColor("#FFD700")
    private val colorErr  = Color.parseColor("#FF4444")
    private val colorCyan = Color.parseColor("#00E5FF")
    private val colorCyan2= Color.parseColor("#80F0FF")

    override fun inflateBinding(inflater: LayoutInflater, container: ViewGroup?): FragmentHomeBinding =
        FragmentHomeBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupObservers()
        startSimulation()
    }

    private fun setupObservers() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    viewModel.connectionState.collectLatest { state ->
                        updateConnectionRow(state)
                    }
                }
                launch {
                    viewModel.robotStatus.collectLatest { status ->
                        if (status.ipAddress.isNotEmpty()) {
                            binding.homeStatusText.text = "SYS: ONLINE · ${status.ipAddress}:9090"
                        }
                    }
                }
            }
        }
    }

    private fun updateConnectionRow(state: ROSBridgeManager.ConnectionState) {
        when (state) {
            ROSBridgeManager.ConnectionState.CONNECTED -> {
                binding.homeStatusDot.setBackgroundResource(R.drawable.shape_circle_green)
                binding.homeRosNodes.text = "14 ROS nodes"
            }
            ROSBridgeManager.ConnectionState.CONNECTING -> {
                binding.homeStatusDot.setBackgroundResource(R.drawable.shape_circle_orange)
                binding.homeStatusText.text = "SYS: CONNECTING…"
                binding.homeRosNodes.text = "-- ROS nodes"
            }
            else -> {
                binding.homeStatusDot.setBackgroundResource(R.drawable.shape_circle_red)
                binding.homeStatusText.text = "SYS: OFFLINE"
                binding.homeRosNodes.text = "-- ROS nodes"
            }
        }
    }

    private fun startSimulation() {
        val tick = object : Runnable {
            override fun run() {
                if (!isAdded) return
                tickMetrics()
                renderMetrics()
                handler.postDelayed(this, 750L)
            }
        }
        handler.post(tick)
    }

    private fun tickMetrics() {
        val r = Random.Default
        battMain = (battMain - 0.01f).coerceIn(30f, 100f)
        battArm  = (battArm  + r.nextFloat() * 0.04f - 0.02f).coerceIn(30f, 100f)
        for (i in cores.indices) {
            cores[i] = (cores[i] + r.nextFloat() * 10f - 5f).coerceIn(8f, 96f)
        }
        rpiTemp = (rpiTemp + r.nextFloat() * 0.8f - 0.38f).coerceIn(44f, 72f)
        rpiRam  = (rpiRam  + r.nextFloat() * 0.08f - 0.04f).coerceIn(1f, 3.8f)
        wsGpu   = (wsGpu   + r.nextFloat() * 14f - 7f).coerceIn(25f, 98f)
        wsVram  = (wsVram  + r.nextFloat() * 0.24f - 0.12f).coerceIn(2f, 7.8f)
        wsCpu   = (wsCpu   + r.nextFloat() * 12f - 6f).coerceIn(15f, 92f)
        wsRam   = (wsRam   + r.nextFloat() * 0.4f - 0.2f).coerceIn(12f, 31f)
        netUp   = (netUp   + r.nextFloat() * 0.5f - 0.25f).coerceIn(0.05f, 8f)
        netDown = (netDown + r.nextFloat() * 0.6f - 0.3f).coerceIn(0.05f, 8f)
        vlaLoad = (vlaLoad + r.nextFloat() * 6f - 3f).coerceIn(20f, 95f)
    }

    private fun renderMetrics() {
        // Battery
        setBar(binding.barBattMain, binding.valBattMain, battMain / 100f, "${battMain.fmt()}%",
            batteryColor(battMain))
        setBar(binding.barBattArm,  binding.valBattArm,  battArm  / 100f, "${battArm.fmt()}%",
            batteryColor(battArm))

        // RPi cores
        setBar(binding.barCore1, binding.valCore1, cores[0] / 100f, "${cores[0].roundToInt()}%", colorCyan)
        setBar(binding.barCore2, binding.valCore2, cores[1] / 100f, "${cores[1].roundToInt()}%", colorCyan)
        setBar(binding.barCore3, binding.valCore3, cores[2] / 100f, "${cores[2].roundToInt()}%", colorCyan)
        setBar(binding.barCore4, binding.valCore4, cores[3] / 100f, "${cores[3].roundToInt()}%", colorCyan)

        // RPi RAM
        setBar(binding.barRpiRam, binding.valRpiRam, rpiRam / 4f, "${rpiRam.fmt()}/4.0 GB", colorCyan)

        // RPi temp
        val tempColor = when {
            rpiTemp > 65f -> colorErr
            rpiTemp > 55f -> colorWarn
            else          -> colorOk
        }
        binding.valRpiTemp.text = "${rpiTemp.fmt()}°C"
        binding.valRpiTemp.setTextColor(tempColor)

        // Workstation
        val gpuColor = when {
            wsGpu > 85f -> colorErr
            wsGpu > 65f -> colorWarn
            else        -> colorCyan
        }
        setBar(binding.barWsGpu,  binding.valWsGpu,  wsGpu / 100f,    "${wsGpu.roundToInt()}%", gpuColor)
        setBar(binding.barWsVram, binding.valWsVram, wsVram / 8f,     "${wsVram.fmt()}/8.0 GB", colorCyan2)
        setBar(binding.barWsCpu,  binding.valWsCpu,  wsCpu / 100f,    "${wsCpu.roundToInt()}%", colorCyan)
        setBar(binding.barWsRam,  binding.valWsRam,  wsRam / 32f,     "${wsRam.fmt()}/32 GB",  colorCyan)

        binding.valNetUp.text   = "↑ ${netUp.fmt()} MB/s"
        binding.valNetDown.text = "↓ ${netDown.fmt()} MB/s"

        // VLA
        setBar(binding.barVlaLoad, binding.valVlaLoad, vlaLoad / 100f, "${vlaLoad.roundToInt()}%", colorCyan2)
    }

    private fun setBar(bar: ProgressBar, label: TextView, fraction: Float, text: String, color: Int) {
        bar.progress = (fraction * 100f).roundToInt().coerceIn(0, 100)
        bar.progressTintList = ColorStateList.valueOf(color)
        label.text = text
        label.setTextColor(color)
    }

    private fun batteryColor(pct: Float) = when {
        pct > 50f -> colorOk
        pct > 25f -> colorWarn
        else      -> colorErr
    }

    private fun Float.fmt() = "%.1f".format(this)

    override fun onDestroyView() {
        handler.removeCallbacksAndMessages(null)
        super.onDestroyView()
    }
}
