package com.varunvaidhiya.robotcontrol.data.repository

import com.varunvaidhiya.robotcontrol.data.models.*
import com.varunvaidhiya.robotcontrol.network.ROSBridgeListener
import com.varunvaidhiya.robotcontrol.network.ROSBridgeManager
import com.varunvaidhiya.robotcontrol.ui.views.PointCloudView
import com.varunvaidhiya.robotcontrol.utils.Constants
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import timber.log.Timber
import kotlin.math.atan2
import kotlin.math.sqrt
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Single source of truth for all robot data.
 * Manages ROSBridge connection and exposes data as StateFlows.
 */
@Singleton
class RobotRepository @Inject constructor() {

    private var rosManager: ROSBridgeManager? = null

    // ── Connection ─────────────────────────────────────────────────────────────
    private val _connectionState = MutableStateFlow(ROSBridgeManager.ConnectionState.DISCONNECTED)
    val connectionState: StateFlow<ROSBridgeManager.ConnectionState> = _connectionState.asStateFlow()

    // ── Odometry (/odom) ───────────────────────────────────────────────────────
    private val _odomData = MutableStateFlow(OdomData())
    val odomData: StateFlow<OdomData> = _odomData.asStateFlow()

    // ── SLAM Map (/map) ────────────────────────────────────────────────────────
    private val _mapData = MutableStateFlow(MapData())
    val mapData: StateFlow<MapData> = _mapData.asStateFlow()

    // ── Robot Status ───────────────────────────────────────────────────────────
    private val _robotStatus = MutableStateFlow(RobotStatus())
    val robotStatus: StateFlow<RobotStatus> = _robotStatus.asStateFlow()

    // ── Motor / Wheel data ─────────────────────────────────────────────────────
    private val _motorData = MutableStateFlow(MotorData(MotorStatus(), MotorStatus(), MotorStatus(), MotorStatus()))
    val motorData: StateFlow<MotorData> = _motorData.asStateFlow()

    private val _wheelSpeeds = MutableStateFlow(WheelSpeed())
    val wheelSpeeds: StateFlow<WheelSpeed> = _wheelSpeeds.asStateFlow()

    // ── Arm joints (/arm/joint_states) ────────────────────────────────────────
    private val _armJointPositions = MutableStateFlow(DoubleArray(Constants.ARM_JOINT_NAMES.size) { 0.0 })
    val armJointPositions: StateFlow<DoubleArray> = _armJointPositions.asStateFlow()

    // ── Mission status (/mission/status) ──────────────────────────────────────
    private val _missionStatus = MutableStateFlow("")
    val missionStatus: StateFlow<String> = _missionStatus.asStateFlow()

    // ── AI orchestration (/ai/status, /ai/response_needed) ────────────────────
    private val _aiStatus = MutableStateFlow("")
    val aiStatus: StateFlow<String> = _aiStatus.asStateFlow()

    private val _aiResponseNeeded = MutableStateFlow("")
    val aiResponseNeeded: StateFlow<String> = _aiResponseNeeded.asStateFlow()

    // ── Point cloud (/camera/depth/points) ────────────────────────────────────
    private val _pointCloud = MutableStateFlow<List<PointCloudView.Point3D>>(emptyList())
    val pointCloud: StateFlow<List<PointCloudView.Point3D>> = _pointCloud.asStateFlow()

    // ── Dataset recording state ────────────────────────────────────────────────
    private val _isRecording = MutableStateFlow(false)
    val isRecording: StateFlow<Boolean> = _isRecording.asStateFlow()

    // ── ROSBridge listener ─────────────────────────────────────────────────────
    private val rosListener = object : ROSBridgeListener {
        override fun onConnected() {
            _connectionState.value = ROSBridgeManager.ConnectionState.CONNECTED
            _robotStatus.value = _robotStatus.value.copy(isConnected = true)
            subscribeToTopics()
        }

        override fun onDisconnected() {
            _connectionState.value = ROSBridgeManager.ConnectionState.DISCONNECTED
            _robotStatus.value = _robotStatus.value.copy(isConnected = false)
        }

        override fun onError(error: String) {
            _connectionState.value = ROSBridgeManager.ConnectionState.ERROR
            Timber.e("ROS Error: $error")
        }

        override fun onMessageReceived(topic: String, message: Map<String, Any>) {
            handleMessage(topic, message)
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    fun connect(ip: String = Constants.DEFAULT_ROBOT_IP, port: Int = Constants.DEFAULT_ROSBRIDGE_PORT) {
        _robotStatus.value = _robotStatus.value.copy(ipAddress = ip)
        rosManager = ROSBridgeManager("ws://$ip:$port", rosListener)
        rosManager?.connect()
        _connectionState.value = ROSBridgeManager.ConnectionState.CONNECTING
    }

    fun disconnect() = rosManager?.disconnect()

    fun sendVelocity(command: VelocityCommand) =
        rosManager?.publish(Constants.TOPIC_CMD_VEL, command.toROSTwist())

    fun sendEmergencyStop() =
        rosManager?.publish(Constants.TOPIC_EMERGENCY_STOP, mapOf("data" to true))

    fun sendArmJointCommand(positions: DoubleArray) {
        rosManager?.publish(
            Constants.TOPIC_ARM_JOINT_COMMANDS,
            mapOf(
                "name"     to Constants.ARM_JOINT_NAMES,
                "position" to positions.toList(),
                "velocity" to emptyList<Double>(),
                "effort"   to emptyList<Double>()
            )
        )
    }

    fun setArmEnabled(enabled: Boolean) =
        rosManager?.publish(Constants.TOPIC_ARM_ENABLE, mapOf("data" to enabled))

    fun sendMode(mode: RobotMode) {
        // Publish to /robot_mode for external monitoring AND to /control_mode
        // so cmd_vel_mux switches the active velocity source.
        rosManager?.publish(Constants.TOPIC_ROBOT_MODE, mapOf("data" to mode.name))
        val muxMode = when (mode) {
            RobotMode.TELEOP     -> "teleop"
            RobotMode.AUTONOMOUS -> "nav2"
            RobotMode.MANUAL     -> "teleop"
        }
        rosManager?.publish(Constants.TOPIC_CONTROL_MODE, mapOf("data" to muxMode))
        _robotStatus.value = _robotStatus.value.copy(currentMode = mode)
    }

    /** Set the cmd_vel_mux control mode directly ("nav2"|"vla"|"teleop"|"rl_nav"). */
    fun sendControlMode(mode: String) =
        rosManager?.publish(Constants.TOPIC_CONTROL_MODE, mapOf("data" to mode))

    /** Send a natural-language / mission command to the mission planner. */
    fun sendMissionCommand(command: String) =
        rosManager?.publish(Constants.TOPIC_MISSION_COMMAND, mapOf("data" to command))

    /** Send a natural-language command to the LangGraph AI orchestration node. */
    fun sendAICommand(command: String) =
        rosManager?.publish(Constants.TOPIC_AI_COMMAND, mapOf("data" to command))

    /** Start rosbag2 recording (name without extension). */
    fun startRecording(bagName: String) {
        rosManager?.publish(Constants.TOPIC_RECORD_START, mapOf("data" to bagName))
        _isRecording.value = true
    }

    /** Stop rosbag2 recording. */
    fun stopRecording() {
        // Send an empty message to the stop topic
        rosManager?.publish(Constants.TOPIC_RECORD_STOP, emptyMap())
        _isRecording.value = false
    }

    // ── Subscriptions ──────────────────────────────────────────────────────────

    private fun subscribeToTopics() {
        rosManager?.apply {
            subscribe(Constants.TOPIC_ODOM,               "nav_msgs/Odometry")
            subscribe(Constants.TOPIC_MAP,                 "nav_msgs/OccupancyGrid")
            subscribe(Constants.TOPIC_IMU,                 "sensor_msgs/Imu")
            subscribe(Constants.TOPIC_DIAGNOSTICS,         "diagnostic_msgs/DiagnosticArray")
            subscribe(Constants.TOPIC_ARM_JOINT_STATES,    "sensor_msgs/JointState")
            subscribe(Constants.TOPIC_MISSION_STATUS,      "std_msgs/String")
            subscribe(Constants.TOPIC_POINT_CLOUD,         "sensor_msgs/PointCloud2")
            subscribe(Constants.TOPIC_ROBOT_STATUS,        "robot_msgs/RobotStatus")
            subscribe(Constants.TOPIC_WHEEL_SPEEDS,        "robot_msgs/WheelSpeed")
            subscribe(Constants.TOPIC_MOTOR_PWM,           "robot_msgs/MotorData")
            // AI orchestration feedback (no-ops if langchain_agent_node not running)
            subscribe(Constants.TOPIC_AI_STATUS,           "std_msgs/String")
            subscribe(Constants.TOPIC_AI_RESPONSE_NEEDED,  "std_msgs/String")
        }
    }

    private fun handleMessage(topic: String, message: Map<String, Any>) {
        try {
            when (topic) {
                Constants.TOPIC_ODOM               -> parseOdom(message)
                Constants.TOPIC_MAP                -> parseMap(message)
                Constants.TOPIC_ARM_JOINT_STATES   -> parseArmJointStates(message)
                Constants.TOPIC_MISSION_STATUS     -> parseMissionStatus(message)
                Constants.TOPIC_POINT_CLOUD        -> parsePointCloud(message)
                Constants.TOPIC_ROBOT_STATUS       -> parseRobotStatus(message)
                Constants.TOPIC_WHEEL_SPEEDS       -> parseWheelSpeeds(message)
                Constants.TOPIC_MOTOR_PWM          -> parseMotorData(message)
                Constants.TOPIC_AI_STATUS          -> parseStringTopic(message) { _aiStatus.value = it }
                Constants.TOPIC_AI_RESPONSE_NEEDED -> parseStringTopic(message) { _aiResponseNeeded.value = it }
            }
        } catch (e: Exception) {
            Timber.e(e, "Error parsing message for topic: $topic")
        }
    }

    private inline fun parseStringTopic(msg: Map<String, Any>, crossinline update: (String) -> Unit) {
        val data = msg["data"] as? String ?: return
        update(data)
    }

    // ── Parsers ────────────────────────────────────────────────────────────────

    @Suppress("UNCHECKED_CAST")
    private fun parseOdom(msg: Map<String, Any>) {
        val pose        = (msg["pose"] as? Map<String, Any>)?.get("pose")  as? Map<String, Any> ?: return
        val position    = pose["position"]    as? Map<String, Any> ?: return
        val orientation = pose["orientation"] as? Map<String, Any> ?: return
        val twist       = (msg["twist"] as? Map<String, Any>)?.get("twist") as? Map<String, Any> ?: return
        val linear      = twist["linear"]  as? Map<String, Any> ?: return
        val angular     = twist["angular"] as? Map<String, Any> ?: return

        val x   = (position["x"] as? Number)?.toFloat() ?: 0f
        val y   = (position["y"] as? Number)?.toFloat() ?: 0f
        val qx  = (orientation["x"] as? Number)?.toDouble() ?: 0.0
        val qy  = (orientation["y"] as? Number)?.toDouble() ?: 0.0
        val qz  = (orientation["z"] as? Number)?.toDouble() ?: 0.0
        val qw  = (orientation["w"] as? Number)?.toDouble() ?: 1.0
        val yaw = atan2(2.0 * (qw * qz + qx * qy), 1.0 - 2.0 * (qy * qy + qz * qz)).toFloat()

        _odomData.value = OdomData(
            x  = x, y  = y, yaw = yaw,
            vx = (linear["x"]  as? Number)?.toFloat() ?: 0f,
            vy = (linear["y"]  as? Number)?.toFloat() ?: 0f,
            vz = (angular["z"] as? Number)?.toFloat() ?: 0f
        )
    }

    @Suppress("UNCHECKED_CAST")
    private fun parseMap(msg: Map<String, Any>) {
        val info   = msg["info"] as? Map<String, Any> ?: return
        val width  = (info["width"]      as? Number)?.toInt()   ?: return
        val height = (info["height"]     as? Number)?.toInt()   ?: return
        val res    = (info["resolution"] as? Number)?.toFloat() ?: 0.05f
        val origin    = info["origin"] as? Map<String, Any>
        val originPos = origin?.get("position") as? Map<String, Any>
        val originX   = (originPos?.get("x") as? Number)?.toFloat() ?: 0f
        val originY   = (originPos?.get("y") as? Number)?.toFloat() ?: 0f
        val rawData   = msg["data"] as? List<Number> ?: return
        _mapData.value = MapData(
            width = width, height = height,
            resolution = res, originX = originX, originY = originY,
            cells = IntArray(rawData.size) { rawData[it].toInt() }
        )
    }

    private fun parseMissionStatus(msg: Map<String, Any>) {
        val status = msg["data"] as? String ?: return
        _missionStatus.value = status
    }

    /**
     * Parses a sensor_msgs/PointCloud2 message delivered via ROSBridge.
     * ROSBridge encodes binary fields as base64, so we decode the raw
     * "data" string and read x/y/z floats at the offsets defined by "fields".
     */
    @Suppress("UNCHECKED_CAST")
    private fun parsePointCloud(msg: Map<String, Any>) {
        // ROSBridge sends PointCloud2 data as a base64-encoded string
        val dataB64 = msg["data"] as? String ?: return
        val bytes   = android.util.Base64.decode(dataB64, android.util.Base64.DEFAULT)

        val fields      = msg["fields"] as? List<Map<String, Any>> ?: return
        val pointStep   = (msg["point_step"] as? Number)?.toInt() ?: return
        val rowStep     = (msg["row_step"]   as? Number)?.toInt() ?: return
        val width       = (msg["width"]      as? Number)?.toInt() ?: return
        val height      = (msg["height"]     as? Number)?.toInt() ?: 1

        // Find byte offsets for x, y, z
        fun fieldOffset(name: String) =
            fields.firstOrNull { (it["name"] as? String) == name }
                ?.let { (it["offset"] as? Number)?.toInt() } ?: -1

        val xOff = fieldOffset("x"); if (xOff < 0) return
        val yOff = fieldOffset("y"); if (yOff < 0) return
        val zOff = fieldOffset("z"); if (zOff < 0) return

        val buf   = java.nio.ByteBuffer.wrap(bytes).order(java.nio.ByteOrder.LITTLE_ENDIAN)
        val total = width * height
        val step  = maxOf(1, total / Constants.POINT_CLOUD_MAX_POINTS)  // subsample
        val pts   = ArrayList<PointCloudView.Point3D>(minOf(total, Constants.POINT_CLOUD_MAX_POINTS))

        for (row in 0 until height) {
            for (col in 0 until width step step) {
                val base = row * rowStep + col * pointStep
                if (base + zOff + 4 > bytes.size) continue
                val x = buf.getFloat(base + xOff)
                val y = buf.getFloat(base + yOff)
                val z = buf.getFloat(base + zOff)
                if (x.isNaN() || y.isNaN() || z.isNaN()) continue
                // Filter out degenerate zero points
                if (x == 0f && y == 0f && z == 0f) continue
                pts.add(PointCloudView.Point3D(x, y, z))
            }
        }

        _pointCloud.value = pts
    }

    private fun parseRobotStatus(msg: Map<String, Any>) {
        val isConnected = msg["connected"] as? Boolean ?: true
        _robotStatus.value = _robotStatus.value.copy(isConnected = isConnected, timestamp = System.currentTimeMillis())
    }

    private fun parseWheelSpeeds(msg: Map<String, Any>) {
        _wheelSpeeds.value = WheelSpeed(
            frontLeft  = (msg["front_left"]  as? Number)?.toFloat() ?: 0f,
            frontRight = (msg["front_right"] as? Number)?.toFloat() ?: 0f,
            backLeft   = (msg["back_left"]   as? Number)?.toFloat() ?: 0f,
            backRight  = (msg["back_right"]  as? Number)?.toFloat() ?: 0f
        )
    }

    @Suppress("UNCHECKED_CAST")
    private fun parseArmJointStates(msg: Map<String, Any>) {
        val names     = (msg["name"]     as? List<*>)?.filterIsInstance<String>() ?: return
        val positions = (msg["position"] as? List<*>)?.map { (it as? Number)?.toDouble() ?: 0.0 } ?: return
        val nameToPos = names.zip(positions).toMap()
        _armJointPositions.value = DoubleArray(Constants.ARM_JOINT_NAMES.size) { i ->
            nameToPos[Constants.ARM_JOINT_NAMES[i]] ?: _armJointPositions.value[i]
        }
    }

    private fun parseMotorData(msg: Map<String, Any>) {
        fun motorStatus(key: String): MotorStatus {
            @Suppress("UNCHECKED_CAST")
            val m = msg[key] as? Map<String, Any> ?: return MotorStatus()
            return MotorStatus(
                pwmValue    = (m["pwm"]         as? Number)?.toInt()   ?: 0,
                rpm         = (m["rpm"]         as? Number)?.toFloat() ?: 0f,
                current     = (m["current"]     as? Number)?.toFloat() ?: 0f,
                temperature = (m["temperature"] as? Number)?.toFloat() ?: 0f
            )
        }
        _motorData.value = MotorData(
            frontLeft  = motorStatus("front_left"),
            frontRight = motorStatus("front_right"),
            backLeft   = motorStatus("back_left"),
            backRight  = motorStatus("back_right")
        )
    }
}
