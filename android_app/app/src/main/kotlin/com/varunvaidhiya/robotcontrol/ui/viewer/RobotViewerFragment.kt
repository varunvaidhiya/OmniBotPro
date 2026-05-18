package com.varunvaidhiya.robotcontrol.ui.viewer

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.ComposeView
import androidx.compose.ui.platform.ViewCompositionStrategy
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import com.varunvaidhiya.robotcontrol.network.ROSBridgeManager
import com.varunvaidhiya.robotcontrol.data.models.OdomData
import dagger.hilt.android.AndroidEntryPoint
import io.github.sceneview.Scene
import io.github.sceneview.rememberEngine
import io.github.sceneview.rememberModelLoader
import io.github.sceneview.rememberModelInstance
import dev.romainguy.kotlin.math.Float3
import kotlin.math.roundToInt

@AndroidEntryPoint
class RobotViewerFragment : Fragment() {

    private val viewModel: RobotViewerViewModel by viewModels()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        return ComposeView(requireContext()).apply {
            setViewCompositionStrategy(ViewCompositionStrategy.DisposeOnViewTreeLifecycleDestroyed)
            setContent {
                MaterialTheme {
                    RobotViewerScreen(viewModel)
                }
            }
        }
    }
}

@Composable
fun RobotViewerScreen(viewModel: RobotViewerViewModel) {
    val connectionState by viewModel.connectionState.collectAsState(initial = ROSBridgeManager.ConnectionState.DISCONNECTED)
    val odomData by viewModel.odomData.collectAsState(initial = OdomData())
    val armJointPositions by viewModel.armJointPositions.collectAsState(initial = DoubleArray(6))

    val engine = rememberEngine()
    val modelLoader = rememberModelLoader(engine)
    val robotInstance = rememberModelInstance(modelLoader, "robot.glb")

    Box(modifier = Modifier.fillMaxSize().background(Color(0xFF050510))) {
        io.github.sceneview.SceneView(
            modifier = Modifier.fillMaxSize(),
            engine = engine,
            modelLoader = modelLoader,
            onGestureListener = io.github.sceneview.rememberOnGestureListener(
                onSingleTapConfirmed = { event, node ->
                    // Handle tap
                }
            )
        ) {
            robotInstance?.let { instance ->
                ModelNode(
                    modelInstance = instance,
                    position = Float3(odomData.x.toFloat(), 0f, -odomData.y.toFloat()),
                    rotation = Float3(0f, Math.toDegrees(odomData.yaw.toDouble()).toFloat(), 0f)
                )
            }
        }

        // Overlay UI
        Column(
            modifier = Modifier
                .align(Alignment.BottomStart)
                .fillMaxWidth()
                .background(Color(0xCC000000))
                .padding(12.dp)
        ) {
            Row(modifier = Modifier.padding(bottom = 8.dp)) {
                Text("POSE", color = Color(0xFF64B5F6), fontSize = 10.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(end = 12.dp))
                Text("X: %.2f".format(odomData.x), color = Color.White, fontFamily = FontFamily.Monospace, fontSize = 11.sp, modifier = Modifier.weight(1f))
                Text("Y: %.2f".format(odomData.y), color = Color.White, fontFamily = FontFamily.Monospace, fontSize = 11.sp, modifier = Modifier.weight(1f))
                Text("θ: ${Math.toDegrees(odomData.yaw.toDouble()).roundToInt()}°", color = Color.White, fontFamily = FontFamily.Monospace, fontSize = 11.sp, modifier = Modifier.weight(1f))
            }
            Row {
                Text("ARM", color = Color(0xFF64B5F6), fontSize = 10.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(end = 12.dp))
                val jointsStr = armJointPositions.joinToString("  ") { "${Math.toDegrees(it).roundToInt()}°" }
                Text(jointsStr, color = Color.White, fontFamily = FontFamily.Monospace, fontSize = 11.sp)
            }
            Text("Tap floor plane to send Nav2 goal · Pinch to orbit", color = Color(0x99FFFFFF), fontSize = 10.sp, modifier = Modifier.padding(top = 6.dp))
        }

        // Status indicator
        Row(
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(12.dp)
                .background(Color(0xAA000000))
                .padding(6.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            val statusColor = when (connectionState) {
                ROSBridgeManager.ConnectionState.CONNECTED -> Color.Green
                ROSBridgeManager.ConnectionState.CONNECTING -> Color(0xFFFFA500)
                else -> Color.Red
            }
            Box(modifier = Modifier.size(8.dp).background(statusColor, CircleShape))
            Spacer(modifier = Modifier.width(6.dp))
            Text(if (robotInstance != null) "Model loaded" else "Loading model...", color = Color.White, fontSize = 11.sp)
        }
        
        if (robotInstance == null) {
            // Optional: fallback UI if no model
        }
    }
}
