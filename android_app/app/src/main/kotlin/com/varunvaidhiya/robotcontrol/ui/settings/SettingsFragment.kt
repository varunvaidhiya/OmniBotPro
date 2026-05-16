package com.varunvaidhiya.robotcontrol.ui.settings

import android.content.res.ColorStateList
import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.SeekBar
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.varunvaidhiya.robotcontrol.databinding.FragmentSettingsBinding
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import java.util.Locale

@AndroidEntryPoint
class SettingsFragment : BaseFragment<FragmentSettingsBinding>() {

    private val viewModel: SettingsViewModel by viewModels()

    private val colorCyan = Color.parseColor("#00E5FF")
    private val colorOk   = Color.parseColor("#00FF7F")

    // Seekbar ranges
    private val maxLinRange  = 0.05f..0.50f   // m/s
    private val maxAngRange  = 0.20f..2.00f   // r/s
    private val sensitivityRange = 0.5f..2.0f

    override fun inflateBinding(inflater: LayoutInflater, container: ViewGroup?): FragmentSettingsBinding =
        FragmentSettingsBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupObservers()
        setupSeekBars()
        setupListeners()
    }

    private fun setupObservers() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    viewModel.robotIp.collectLatest { ip ->
                        if (binding.editIp.text.isNullOrEmpty()) binding.editIp.setText(ip)
                    }
                }
                launch {
                    viewModel.robotPort.collectLatest { port ->
                        if (binding.editPort.text.isNullOrEmpty()) binding.editPort.setText(port.toString())
                    }
                }
            }
        }
    }

    private fun setupSeekBars() {
        binding.seekMaxLinear.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(sb: SeekBar, p: Int, fromUser: Boolean) {
                val v = lerp(maxLinRange, p / 100f)
                binding.valMaxLinear.text = String.format(Locale.US, "%.2f m/s", v)
            }
            override fun onStartTrackingTouch(sb: SeekBar) = Unit
            override fun onStopTrackingTouch(sb: SeekBar) = Unit
        })

        binding.seekMaxAngular.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(sb: SeekBar, p: Int, fromUser: Boolean) {
                val v = lerp(maxAngRange, p / 100f)
                binding.valMaxAngular.text = String.format(Locale.US, "%.2f r/s", v)
            }
            override fun onStartTrackingTouch(sb: SeekBar) = Unit
            override fun onStopTrackingTouch(sb: SeekBar) = Unit
        })

        binding.seekSensitivity.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(sb: SeekBar, p: Int, fromUser: Boolean) {
                val v = lerp(sensitivityRange, p / 100f)
                binding.valSensitivity.text = String.format(Locale.US, "×%.1f", v)
            }
            override fun onStartTrackingTouch(sb: SeekBar) = Unit
            override fun onStopTrackingTouch(sb: SeekBar) = Unit
        })
    }

    private fun setupListeners() {
        binding.btnSave.setOnClickListener {
            val ip = binding.editIp.text.toString()
            val portStr = binding.editPort.text.toString()

            if (ip.isBlank()) {
                binding.layoutIp.error = "Required"; return@setOnClickListener
            }
            val port = portStr.toIntOrNull()
            if (port == null) {
                binding.layoutPort.error = "Invalid port"; return@setOnClickListener
            }

            binding.layoutIp.error = null
            binding.layoutPort.error = null

            viewModel.saveConnectionSettings(ip, port)
            flashSaved()
        }
    }

    private fun flashSaved() {
        val okTint = ColorStateList.valueOf(colorOk)
        val cyanTint = ColorStateList.valueOf(colorCyan)

        binding.btnSave.text = "✓ SAVED"
        binding.btnSave.setTextColor(colorOk)
        binding.btnSave.strokeColor = okTint

        binding.root.postDelayed({
            if (view != null) {
                binding.btnSave.text = "SAVE CONFIG"
                binding.btnSave.setTextColor(colorCyan)
                binding.btnSave.strokeColor = cyanTint
            }
        }, 2000L)
    }

    private fun lerp(range: ClosedFloatingPointRange<Float>, t: Float): Float =
        range.start + (range.endInclusive - range.start) * t.coerceIn(0f, 1f)
}
