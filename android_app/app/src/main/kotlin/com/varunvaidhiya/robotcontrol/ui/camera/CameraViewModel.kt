package com.varunvaidhiya.robotcontrol.ui.camera

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.varunvaidhiya.robotcontrol.data.repository.RobotRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class CameraViewModel @Inject constructor(
    private val repository: RobotRepository
) : ViewModel() {

    val connectionState = repository.connectionState

    // In a real app, this might come from a "camera_info" topic or settings
    // Defaulting to standard ros_web_video_server URL pattern
    private val _streamUrl = MutableStateFlow("")
    val streamUrl: StateFlow<String> = _streamUrl.asStateFlow()

    init {
        viewModelScope.launch {
            repository.robotStatus.collect { status ->
                if (status.isConnected && status.ipAddress.isNotEmpty()) {
                    // ros_web_video_server default port is 8080.
                    // /camera/front/image_raw is the forward base camera
                    // (the old /camera/image_raw topic no longer exists).
                    // Other streams: /camera/base/bev/image_raw (top-down BEV),
                    // /camera/color/image_raw (Astra RGB, rear-facing),
                    // /camera/wrist/image_raw (gripper cam).
                    val url = "http://${status.ipAddress}:8080/stream?topic=/camera/front/image_raw&type=mjpeg&quality=50"
                    _streamUrl.value = url
                }
            }
        }
    }
}
