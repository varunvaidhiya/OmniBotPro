package com.varunvaidhiya.robotcontrol.ui.controls

import android.animation.ObjectAnimator
import android.animation.PropertyValuesHolder
import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.animation.LinearInterpolator
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.data.models.RobotMode
import com.varunvaidhiya.robotcontrol.databinding.FragmentControlsBinding
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import java.util.Locale

@AndroidEntryPoint
class ControlsFragment : BaseFragment<FragmentControlsBinding>() {

    private val viewModel: ControlsViewModel by viewModels()
    private var estopActive = false
    private var ring1Anim: ObjectAnimator? = null
    private var ring2Anim: ObjectAnimator? = null

    override fun inflateBinding(inflater: LayoutInflater, container: ViewGroup?): FragmentControlsBinding =
        FragmentControlsBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupJoystick()
        setupEmergencyStop()
        setupModeToggle()
        startEstopPulse()
        observeMode()
    }

    // ── Joystick ──────────────────────────────────────────────────────

    private fun setupJoystick() {
        binding.virtualJoystick.onJoystickMoved = { x, y ->
            val maxLinear = 1.0f; val maxAngular = 1.5f
            val linearX = y * maxLinear
            val angularZ = -x * maxAngular

            viewModel.sendVelocity(linearX, 0f, angularZ)

            binding.textValues.text = String.format(Locale.US, "LIN  %+.2f m/s", linearX)
            binding.textAngularValue.text = String.format(Locale.US, "ANG  %+.2f r/s", angularZ)
        }
    }

    // ── Emergency Stop ────────────────────────────────────────────────

    private fun setupEmergencyStop() {
        binding.btnEmergencyStop.setOnClickListener {
            estopActive = !estopActive
            applyEstopState()
            if (estopActive) viewModel.triggerEmergencyStop()
        }
    }

    private fun applyEstopState() {
        val btn = binding.btnEmergencyStop
        if (estopActive) {
            btn.text = "◼ ACTIVE"
            btn.backgroundTintList = android.content.res.ColorStateList.valueOf(Color.parseColor("#33FF4444"))
            ring1Anim?.cancel(); ring2Anim?.cancel()
            binding.estopRing1.alpha = 0f; binding.estopRing2.alpha = 0f
        } else {
            btn.text = "EMRG\nSTOP"
            btn.backgroundTintList = android.content.res.ColorStateList.valueOf(Color.parseColor("#1AFF4444"))
            startEstopPulse()
        }
    }

    private fun startEstopPulse() {
        ring1Anim?.cancel(); ring2Anim?.cancel()

        ring1Anim = ObjectAnimator.ofPropertyValuesHolder(binding.estopRing1,
            PropertyValuesHolder.ofFloat(View.SCALE_X, 1f, 2.2f),
            PropertyValuesHolder.ofFloat(View.SCALE_Y, 1f, 2.2f),
            PropertyValuesHolder.ofFloat(View.ALPHA,   0.5f, 0f)).apply {
            duration = 2000L; repeatCount = ObjectAnimator.INFINITE; interpolator = LinearInterpolator()
        }

        ring2Anim = ObjectAnimator.ofPropertyValuesHolder(binding.estopRing2,
            PropertyValuesHolder.ofFloat(View.SCALE_X, 1f, 2.2f),
            PropertyValuesHolder.ofFloat(View.SCALE_Y, 1f, 2.2f),
            PropertyValuesHolder.ofFloat(View.ALPHA,   0.5f, 0f)).apply {
            duration = 2000L; startDelay = 500L; repeatCount = ObjectAnimator.INFINITE
            interpolator = LinearInterpolator()
        }

        ring1Anim?.start(); ring2Anim?.start()
    }

    // ── Mode toggle ───────────────────────────────────────────────────

    private fun setupModeToggle() {
        binding.toggleMode.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (!isChecked) return@addOnButtonCheckedListener
            val mode = when (checkedId) {
                R.id.btn_teleop     -> RobotMode.TELEOP
                R.id.btn_autonomous -> RobotMode.AUTONOMOUS
                R.id.btn_manual     -> RobotMode.MANUAL
                else                -> return@addOnButtonCheckedListener
            }
            viewModel.setMode(mode)
            binding.virtualJoystick.isEnabled = (mode != RobotMode.AUTONOMOUS)
        }
    }

    private fun observeMode() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.robotStatus.collectLatest { status ->
                    val targetId = when (status.currentMode) {
                        RobotMode.TELEOP     -> R.id.btn_teleop
                        RobotMode.AUTONOMOUS -> R.id.btn_autonomous
                        RobotMode.MANUAL     -> R.id.btn_manual
                    }
                    if (binding.toggleMode.checkedButtonId != targetId) {
                        binding.toggleMode.check(targetId)
                    }
                    binding.virtualJoystick.isEnabled = (status.currentMode != RobotMode.AUTONOMOUS)
                }
            }
        }
    }

    override fun onDestroyView() {
        ring1Anim?.cancel(); ring2Anim?.cancel()
        super.onDestroyView()
    }
}
