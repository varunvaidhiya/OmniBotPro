package com.varunvaidhiya.robotcontrol.ui.ai

import android.animation.ObjectAnimator
import android.animation.PropertyValuesHolder
import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.os.Handler
import android.os.Looper
import android.view.inputmethod.EditorInfo
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.databinding.FragmentAiBinding
import com.varunvaidhiya.robotcontrol.network.ROSBridgeManager
import com.varunvaidhiya.robotcontrol.ui.common.BaseFragment
import com.varunvaidhiya.robotcontrol.ui.views.JarvisView
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

@AndroidEntryPoint
class AIFragment : BaseFragment<FragmentAiBinding>() {

    private val aiViewModel: AIViewModel by viewModels()
    private val chatAdapter = ChatAdapter()
    private val uiHandler = Handler(Looper.getMainLooper())

    private var isVisualMode = true
    private var currentAiState = JarvisView.AiState.IDLE

    private val colorIdle       = Color.parseColor("#6100E5FF")   // muted cyan
    private val colorProcessing = Color.parseColor("#FFD700")
    private val colorSpeaking   = Color.parseColor("#80F0FF")
    private val colorAccent     = Color.parseColor("#00E5FF")

    override fun inflateBinding(inflater: LayoutInflater, container: ViewGroup?): FragmentAiBinding =
        FragmentAiBinding.inflate(inflater, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupRecycler()
        setupInput()
        setupQuickChips()
        setupViewToggle()
        setupObservers()
        updateModeUi()
    }

    // ── View toggle ──────────────────────────────────────────────────

    private fun setupViewToggle() {
        binding.btnViewVisual.setOnClickListener { setMode(visual = true) }
        binding.btnViewChat.setOnClickListener   { setMode(visual = false) }
    }

    private fun setMode(visual: Boolean) {
        isVisualMode = visual
        updateModeUi()
    }

    private fun updateModeUi() {
        binding.layoutVisualMode.visibility = if (isVisualMode) View.VISIBLE else View.GONE
        binding.recyclerMessages.visibility = if (isVisualMode) View.GONE else View.VISIBLE

        binding.btnViewVisual.apply {
            setBackgroundColor(if (isVisualMode) colorAccent else Color.TRANSPARENT)
            setTextColor(if (isVisualMode) Color.parseColor("#010D18") else colorIdle)
        }
        binding.btnViewChat.apply {
            setBackgroundColor(if (!isVisualMode) colorAccent else Color.TRANSPARENT)
            setTextColor(if (!isVisualMode) Color.parseColor("#010D18") else colorIdle)
        }
    }

    // ── Recycler ─────────────────────────────────────────────────────

    private fun setupRecycler() {
        val lm = LinearLayoutManager(requireContext()).apply { stackFromEnd = true }
        binding.recyclerMessages.layoutManager = lm
        binding.recyclerMessages.adapter = chatAdapter
    }

    // ── Input & chips ─────────────────────────────────────────────────

    private fun setupInput() {
        binding.editCommand.setOnEditorActionListener { _, actionId, _ ->
            if (actionId == EditorInfo.IME_ACTION_SEND) { sendCommand(); true } else false
        }
        binding.btnSend.setOnClickListener { sendCommand() }
    }

    private fun setupQuickChips() {
        binding.chipNavigate.setOnClickListener { prefill("Navigate home") }
        binding.chipExplore.setOnClickListener  { prefill("Explore the area") }
        binding.chipPick.setOnClickListener     { prefill("Pick up the object") }
        binding.chipStop.setOnClickListener     { aiViewModel.sendCommand("stop") }
    }

    // ── Observers ────────────────────────────────────────────────────

    private fun setupObservers() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {

                launch {
                    aiViewModel.messages.collectLatest { msgs ->
                        chatAdapter.submitList(msgs)
                        if (msgs.isNotEmpty()) {
                            binding.recyclerMessages.scrollToPosition(msgs.size - 1)
                        }
                    }
                }

                launch {
                    aiViewModel.connectionState.collectLatest { state ->
                        val dotRes = when (state) {
                            ROSBridgeManager.ConnectionState.CONNECTED   -> R.drawable.shape_circle_green
                            ROSBridgeManager.ConnectionState.CONNECTING  -> R.drawable.shape_circle_orange
                            else                                          -> R.drawable.shape_circle_red
                        }
                        binding.aiStatusDot.setBackgroundResource(dotRes)
                        binding.aiDotHalo.setBackgroundResource(dotRes)
                    }
                }

                launch {
                    aiViewModel.isRecording.collectLatest { rec ->
                        binding.switchRecording.isChecked = rec
                        binding.textRecordingStatus.text =
                            if (rec) "Dataset recording: ON  ⏺" else "Dataset recording: OFF"
                    }
                }
            }
        }
    }

    // ── AI state machine ─────────────────────────────────────────────

    private fun transitionToState(state: JarvisView.AiState) {
        currentAiState = state
        binding.jarvisView.aiState = state

        val (labelText, labelColor, speechText) = when (state) {
            JarvisView.AiState.IDLE       -> Triple("OMNIBOT · IDLE",          colorIdle,       "OMNIBOT ONLINE.\nAWAITING COMMAND.")
            JarvisView.AiState.PROCESSING -> Triple("OMNIBOT · PROCESSING…",   colorProcessing, "PROCESSING\nCOMMAND...")
            JarvisView.AiState.SPEAKING   -> Triple("OMNIBOT · SPEAKING",       colorSpeaking,   "EXECUTING\nSEQUENCE.")
        }
        binding.textAiStateLabel.text = labelText
        binding.textAiStateLabel.setTextColor(labelColor)
        binding.textSpeech.text = speechText
        binding.textSpeech.setTextColor(labelColor)

        // Pulse halo animation when non-idle
        val halo = binding.aiDotHalo
        if (state != JarvisView.AiState.IDLE) {
            val anim = ObjectAnimator.ofPropertyValuesHolder(halo,
                PropertyValuesHolder.ofFloat(View.SCALE_X, 1f, 2.2f),
                PropertyValuesHolder.ofFloat(View.SCALE_Y, 1f, 2.2f),
                PropertyValuesHolder.ofFloat(View.ALPHA, 0.5f, 0f))
            anim.duration = 1800L; anim.repeatCount = ObjectAnimator.INFINITE; anim.start()
            halo.tag = anim
        } else {
            (halo.tag as? ObjectAnimator)?.cancel()
            halo.tag = null
            halo.alpha = 0f; halo.scaleX = 1f; halo.scaleY = 1f
        }

        // Disable chips & input during non-idle
        val enabled = state == JarvisView.AiState.IDLE
        binding.chipNavigate.alpha = if (enabled) 1f else 0.3f
        binding.chipExplore.alpha  = if (enabled) 1f else 0.3f
        binding.chipPick.alpha     = if (enabled) 1f else 0.3f
        binding.chipStop.alpha     = if (enabled) 1f else 0.3f
        binding.editCommand.isEnabled = enabled
        binding.btnSend.alpha = if (enabled) 1f else 0.35f

        // Auto-advance state simulation
        if (state == JarvisView.AiState.PROCESSING) {
            uiHandler.postDelayed({
                if (_binding != null) transitionToState(JarvisView.AiState.SPEAKING)
            }, 1600L)
        } else if (state == JarvisView.AiState.SPEAKING) {
            uiHandler.postDelayed({
                if (_binding != null) transitionToState(JarvisView.AiState.IDLE)
            }, 4500L)
        }
    }

    private fun sendCommand() {
        val text = binding.editCommand.text?.toString()?.trim() ?: return
        if (text.isEmpty() || currentAiState != JarvisView.AiState.IDLE) return
        aiViewModel.sendCommand(text)
        binding.editCommand.setText("")
        transitionToState(JarvisView.AiState.PROCESSING)
    }

    private fun prefill(text: String) {
        if (currentAiState != JarvisView.AiState.IDLE) return
        binding.editCommand.setText(text)
        binding.editCommand.setSelection(text.length)
    }

    override fun onDestroyView() {
        uiHandler.removeCallbacksAndMessages(null)
        super.onDestroyView()
    }
}
