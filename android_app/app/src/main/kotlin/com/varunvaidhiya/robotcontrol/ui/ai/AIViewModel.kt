package com.varunvaidhiya.robotcontrol.ui.ai

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.repository.RobotRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import javax.inject.Inject

data class ChatMessage(
    val text: String,
    val isUser: Boolean,
    val timestamp: String = SimpleDateFormat("HH:mm", Locale.getDefault()).format(Date())
)

@HiltViewModel
class AIViewModel @Inject constructor(
    private val repository: RobotRepository
) : ViewModel() {

    val connectionState = repository.connectionState
    val isRecording     = repository.isRecording

    private val _messages = MutableStateFlow<List<ChatMessage>>(
        listOf(ChatMessage("Hi Varun! I'm ready. Tell me what to do — navigate somewhere, pick an object, or explore the area.", isUser = false))
    )
    val messages: StateFlow<List<ChatMessage>> = _messages.asStateFlow()

    init {
        // Mission status updates from the robot
        viewModelScope.launch {
            repository.missionStatus.collect { status ->
                if (status.isNotBlank()) {
                    appendMessage(ChatMessage(status, isUser = false))
                }
            }
        }
        // AI orchestration status (from langchain_agent_node when running)
        viewModelScope.launch {
            repository.aiStatus.collect { status ->
                if (status.isNotBlank()) {
                    appendMessage(ChatMessage("[AI] $status", isUser = false))
                }
            }
        }
        // Clarification requests from the AI agent
        viewModelScope.launch {
            repository.aiResponseNeeded.collect { question ->
                if (question.isNotBlank()) {
                    appendMessage(ChatMessage("[AI asks] $question", isUser = false))
                }
            }
        }
    }

    fun sendCommand(text: String) {
        if (text.isBlank()) return

        appendMessage(ChatMessage(text, isUser = true))

        // If the text looks like a structured mission command (contains ":"),
        // send it directly to /mission/command (mission_planner). Otherwise send
        // to /ai/command so the LangGraph agent can decompose it. When the
        // langchain_agent_node is not running, /ai/command is a no-op — in that
        // case also send the raw text to /mission/command as a fallback.
        val lower = text.lowercase(Locale.getDefault())
        val isStructured = lower.startsWith("navigate:") || lower.startsWith("vla:") ||
            lower.startsWith("rl_nav:") || lower.startsWith("rl_arm:") ||
            lower.startsWith("go:") || lower.startsWith("stop")

        if (isStructured) {
            repository.sendMissionCommand(text)
            appendMessage(ChatMessage("Sent to mission planner: \"$text\"", isUser = false))
        } else {
            // Send to AI agent for natural language understanding
            repository.sendAICommand(text)
            // Fallback: also build a best-effort structured command
            val fallback = when {
                lower.startsWith("navigate") || lower.startsWith("go to") ->
                    "navigate:${text.substringAfter(" ").trim()}"
                lower.startsWith("pick") || lower.startsWith("grab") ||
                lower.startsWith("find") || lower.startsWith("fetch") ->
                    "vla:$text"
                else -> null
            }
            if (fallback != null) {
                repository.sendMissionCommand(fallback)
            }
            appendMessage(ChatMessage("Got it! Sent to AI agent.", isUser = false))
        }
    }

    fun toggleRecording(enable: Boolean) {
        if (enable) {
            val bagName = "omnibot_${System.currentTimeMillis()}"
            repository.startRecording(bagName)
            appendMessage(ChatMessage("Started dataset recording → $bagName.bag", isUser = false))
        } else {
            repository.stopRecording()
            appendMessage(ChatMessage("Stopped recording. Bag saved.", isUser = false))
        }
    }

    private fun appendMessage(msg: ChatMessage) {
        _messages.value = _messages.value + msg
    }
}
