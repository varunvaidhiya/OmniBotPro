package com.varunvaidhiya.robotcontrol.ui.dashboard

import android.app.Dialog
import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.Window
import android.view.WindowManager
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.data.models.OdomData
import com.varunvaidhiya.robotcontrol.databinding.FragmentDashboardBinding
import com.varunvaidhiya.robotcontrol.network.ROSBridgeManager
import com.varunvaidhiya.robotcontrol.ui.camera.CameraViewModel
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import com.varunvaidhiya.robotcontrol.utils.Constants
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import kotlin.math.roundToInt

@AndroidEntryPoint
class DashboardFragment : BaseFragment<FragmentDashboardBinding>() {

    private val dashboardViewModel: DashboardViewModel by viewModels()
    private val cameraViewModel: CameraViewModel by viewModels()

    private var chartEntryIndex = 0f
    private var bevVisible = false

    override fun inflateBinding(inflater: LayoutInflater, container: ViewGroup?): FragmentDashboardBinding =
        FragmentDashboardBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupChart()
        setupCameraControls()
        setupObservers()
    }

    private fun setupCameraControls() {
        binding.btnBevToggle.setOnClickListener {
            bevVisible = !bevVisible
            binding.bevOverlayContainer.visibility = if (bevVisible) View.VISIBLE else View.GONE
            binding.btnBevToggle.alpha = if (bevVisible) 1f else 0.45f
        }
        binding.btnFullscreen.setOnClickListener { showFullscreenCamera() }
    }

    private fun showFullscreenCamera() {
        val dialog = Dialog(requireContext(), android.R.style.Theme_Black_NoTitleBar_Fullscreen)
        dialog.requestWindowFeature(Window.FEATURE_NO_TITLE)
        dialog.window?.setFlags(
            WindowManager.LayoutParams.FLAG_FULLSCREEN,
            WindowManager.LayoutParams.FLAG_FULLSCREEN
        )
        dialog.setContentView(com.varunvaidhiya.robotcontrol.R.layout.dialog_fullscreen_camera)
        dialog.show()
    }

    private fun setupObservers() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {

                launch {
                    dashboardViewModel.connectionState.collectLatest { state ->
                        updateConnectionStatus(state)
                    }
                }

                launch {
                    dashboardViewModel.robotStatus.collectLatest { status ->
                        binding.textIpAddress.text =
                            "IP: ${if (status.ipAddress.isEmpty()) "--" else status.ipAddress}"
                    }
                }

                launch {
                    cameraViewModel.streamUrl.collectLatest { url ->
                        if (url.isNotEmpty()) {
                            binding.mjpegView.startStream(url)
                        } else {
                            binding.mjpegView.stopStream()
                        }
                    }
                }

                launch {
                    dashboardViewModel.odomData.collectLatest { odom ->
                        updateChart(odom)
                        updateOdomDisplay(odom)
                        // Mirror odom into the BEV map overlay
                        if (bevVisible) {
                            binding.bevSlamView.updateRobotPose(odom.x, odom.y, odom.yaw)
                        }
                    }
                }

                launch {
                    dashboardViewModel.mapData.collectLatest { map ->
                        if (map.cells.isNotEmpty() && bevVisible) {
                            binding.bevSlamView.updateMap(map.width, map.height, map.cells)
                        }
                    }
                }

                launch {
                    dashboardViewModel.isRecording.collectLatest { recording ->
                        binding.textRecBadge.visibility = if (recording) View.VISIBLE else View.GONE
                    }
                }
            }
        }
    }

    private fun setupChart() {
        val colorCyan = Color.parseColor("#00E5FF")
        val colorOk   = Color.parseColor("#00FF7F")
        val colorWarn = Color.parseColor("#FFD700")
        val muted     = Color.parseColor("#6100E5FF")

        val setVx = LineDataSet(ArrayList(), "Vx").apply {
            color = colorCyan; setDrawCircles(false); setDrawValues(false); lineWidth = 1.6f
        }
        val setVy = LineDataSet(ArrayList(), "Vy").apply {
            color = colorOk;   setDrawCircles(false); setDrawValues(false); lineWidth = 1.6f
        }
        val setVz = LineDataSet(ArrayList(), "ω").apply {
            color = colorWarn; setDrawCircles(false); setDrawValues(false); lineWidth = 1.6f
        }

        binding.chartVelocity.apply {
            data = LineData(setVx, setVy, setVz)
            description.isEnabled = false
            setTouchEnabled(false)
            setDrawGridBackground(false)
            axisRight.isEnabled = false
            axisLeft.isEnabled = false
            xAxis.setDrawLabels(false)
            xAxis.setDrawGridLines(false)
            legend.isEnabled = false
            setBackgroundColor(Color.TRANSPARENT)
            setVisibleXRangeMaximum(Constants.CHART_MAX_DATA_POINTS.toFloat())
        }
    }

    private fun updateChart(odom: OdomData) {
        val data = binding.chartVelocity.data ?: return
        val x = chartEntryIndex++
        data.getDataSetByIndex(0).addEntry(Entry(x, odom.vx))
        data.getDataSetByIndex(1).addEntry(Entry(x, odom.vy))
        data.getDataSetByIndex(2).addEntry(Entry(x, odom.vz))
        data.notifyDataChanged()
        binding.chartVelocity.notifyDataSetChanged()
        binding.chartVelocity.moveViewToX(x)
    }

    private fun updateOdomDisplay(odom: OdomData) {
        val sign = { v: Float -> if (v >= 0) "+" else "" }
        binding.textOdomX.text = "X:${sign(odom.x)}%.2fm".format(odom.x)
        binding.textOdomY.text = "Y:${sign(odom.y)}%.2fm".format(odom.y)
        val yawDeg = Math.toDegrees(odom.yaw.toDouble()).roundToInt()
        binding.textOdomYaw.text = "θ:${if (yawDeg >= 0) "+" else ""}${yawDeg}°"

        // Update chart legend
        binding.legendVx.text = "Vx ${sign(odom.vx)}%.2f".format(odom.vx)
        binding.legendVy.text = "Vy ${sign(odom.vy)}%.2f".format(odom.vy)
        binding.legendVw.text = "ω ${sign(odom.vz)}%.2f".format(odom.vz)
    }

    private fun updateConnectionStatus(state: ROSBridgeManager.ConnectionState) {
        val (text, colorRes) = when (state) {
            ROSBridgeManager.ConnectionState.CONNECTED    ->
                "SYS: ONLINE · ws://${binding.textIpAddress.text}"  to R.drawable.shape_circle_green
            ROSBridgeManager.ConnectionState.CONNECTING  ->
                "SYS: CONNECTING…"                                   to R.drawable.shape_circle_orange
            ROSBridgeManager.ConnectionState.DISCONNECTED ->
                "SYS: OFFLINE"                                       to R.drawable.shape_circle_red
            ROSBridgeManager.ConnectionState.ERROR        ->
                "SYS: ERROR"                                         to R.drawable.shape_circle_red
        }
        binding.textConnectionStatus.text = text
        binding.statusIndicator.setBackgroundResource(colorRes)
    }
}
