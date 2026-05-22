package com.varunvaidhiya.robotcontrol.data.repository

import com.varunvaidhiya.robotcontrol.data.models.*
import com.varunvaidhiya.robotcontrol.data.preferences.AppPreferences
import com.varunvaidhiya.robotcontrol.data.remote.AlertManagerApi
import com.varunvaidhiya.robotcontrol.data.remote.LokiApi
import com.varunvaidhiya.robotcontrol.data.remote.PrometheusApi
import com.varunvaidhiya.robotcontrol.data.remote.WandBApi
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ObservabilityRepository @Inject constructor(
    private val prometheusApi: PrometheusApi,
    private val alertManagerApi: AlertManagerApi,
    private val lokiApi: LokiApi,
    private val wandbApi: WandBApi,
    private val prefs: AppPreferences
) {
    // ── Preferences shortcuts ─────────────────────────────────────────────────

    val gpuIp:       Flow<String> = prefs.gpuDesktopIp
    val wandbApiKey: Flow<String> = prefs.wandbApiKey
    val wandbEntity: Flow<String> = prefs.wandbEntity

    // ── Health polling (5 s interval) ─────────────────────────────────────────

    fun healthFlow(): Flow<RobotHealthSnapshot> = prefs.gpuDesktopIp
        .flatMapLatest { ip ->
            flow {
                while (true) {
                    emit(fetchHealthSnapshot(ip))
                    delay(5_000)
                }
            }.flowOn(Dispatchers.IO)
        }

    private suspend fun fetchHealthSnapshot(ip: String): RobotHealthSnapshot = coroutineScope {
        try {
            val piCpu     = async { prometheusApi.scalarOrNull(ip, PI_CPU_QUERY) ?: 0.0 }
            val piRam     = async { prometheusApi.scalarOrNull(ip, PI_RAM_QUERY) ?: 0.0 }
            val piDisk    = async { prometheusApi.scalarOrNull(ip, PI_DISK_QUERY) ?: 0.0 }
            val gpuUtil   = async { prometheusApi.scalarOrNull(ip, GPU_UTIL_QUERY) ?: 0.0 }
            val gpuVram   = async { prometheusApi.scalarOrNull(ip, GPU_VRAM_QUERY) ?: 0.0 }
            val gpuTemp   = async { prometheusApi.scalarOrNull(ip, GPU_TEMP_QUERY) ?: 0.0 }
            val estop     = async { prometheusApi.scalarOrNull(ip, ESTOP_QUERY) ?: 0.0 }
            val mode      = async {
                prometheusApi.labeledScalars(ip, CONTROL_MODE_QUERY, "mode").keys.firstOrNull() ?: "--"
            }
            val driverP50 = async { prometheusApi.scalarOrNull(ip, DRIVER_P50_QUERY) ?: 0.0 }
            val driverP95 = async { prometheusApi.scalarOrNull(ip, DRIVER_P95_QUERY) ?: 0.0 }
            val driverMax = async { prometheusApi.scalarOrNull(ip, DRIVER_MAX_QUERY) ?: 0.0 }
            val vlaMs     = async { prometheusApi.scalarOrNull(ip, VLA_MS_QUERY) ?: 0.0 }
            val rlNavMs   = async { prometheusApi.scalarOrNull(ip, RL_NAV_MS_QUERY) ?: 0.0 }
            val rlArmMs   = async { prometheusApi.scalarOrNull(ip, RL_ARM_MS_QUERY) ?: 0.0 }
            val robotVx   = async { prometheusApi.scalarOrNull(ip, ROBOT_VX_QUERY) ?: 0.0 }
            val robotVy   = async { prometheusApi.scalarOrNull(ip, ROBOT_VY_QUERY) ?: 0.0 }
            val robotOmega= async { prometheusApi.scalarOrNull(ip, ROBOT_OMEGA_QUERY) ?: 0.0 }
            val successRt = async { prometheusApi.scalarOrNull(ip, MISSION_SUCCESS_QUERY) ?: -1.0 }
            val targets   = async {
                prometheusApi.getTargets(ip).data?.activeTargets?.map { t ->
                    TargetStatus(t.labels["job"] ?: t.scrapeUrl, t.health)
                } ?: emptyList()
            }
            val alerts    = async {
                prometheusApi.getAlerts(ip).data?.alerts
                    ?.filter { it.state == "firing" } ?: emptyList()
            }

            val firedAlerts = alerts.await()

            RobotHealthSnapshot(
                piCpuPct          = piCpu.await(),
                piRamPct          = piRam.await(),
                piDiskPct         = piDisk.await(),
                gpuUtilPct        = gpuUtil.await(),
                gpuVramPct        = gpuVram.await(),
                gpuTempC          = gpuTemp.await(),
                estopActive       = estop.await() > 0.5,
                controlMode       = mode.await(),
                driverP50ms       = driverP50.await(),
                driverP95ms       = driverP95.await(),
                driverMaxMs       = driverMax.await(),
                vlaInferenceMs    = vlaMs.await(),
                rlNavMs           = rlNavMs.await(),
                rlArmMs           = rlArmMs.await(),
                robotVx           = robotVx.await(),
                robotVy           = robotVy.await(),
                robotOmega        = robotOmega.await(),
                missionSuccessRate= successRt.await(),
                targets           = targets.await(),
                firingCritical    = firedAlerts.count { it.labels["severity"] == "critical" },
                firingWarning     = firedAlerts.count { it.labels["severity"] == "warning" }
            )
        } catch (e: Exception) {
            RobotHealthSnapshot(error = e.message ?: "Connection failed")
        }
    }

    // ── Range data for velocity chart in HealthFragment ───────────────────────

    fun velocityRangeFlow(ip: String): Flow<Pair<List<Pair<Long, Double>>, List<Pair<Long, Double>>>> =
        flow {
            while (true) {
                val now = System.currentTimeMillis() / 1000L
                val start = now - 300  // 5 minute window
                val vx = fetchRange(ip, ROBOT_VX_QUERY, start, now)
                val vy = fetchRange(ip, ROBOT_VY_QUERY, start, now)
                emit(Pair(vx, vy))
                delay(5_000)
            }
        }.flowOn(Dispatchers.IO)

    private fun fetchRange(ip: String, query: String, start: Long, end: Long): List<Pair<Long, Double>> =
        runCatching {
            val result = prometheusApi.queryRange(ip, query, start, end, "5s")
            result.data?.result?.firstOrNull()?.values?.mapNotNull { v ->
                val ts = (v.getOrNull(0) as? Double)?.toLong() ?: return@mapNotNull null
                val d  = (v.getOrNull(1) as? String)?.toDoubleOrNull() ?: return@mapNotNull null
                Pair(ts, d)
            } ?: emptyList()
        }.getOrDefault(emptyList())

    // ── Alerts ────────────────────────────────────────────────────────────────

    fun alertsFlow(): Flow<List<AmAlert>> = prefs.gpuDesktopIp
        .flatMapLatest { ip ->
            flow {
                while (true) {
                    emit(runCatching { alertManagerApi.getAlerts(ip) }.getOrDefault(emptyList()))
                    delay(10_000)
                }
            }.flowOn(Dispatchers.IO)
        }

    suspend fun createSilence(ip: String, req: CreateSilenceRequest): String? =
        kotlinx.coroutines.withContext(Dispatchers.IO) { alertManagerApi.createSilence(ip, req) }

    suspend fun deleteSilence(ip: String, id: String): Boolean =
        kotlinx.coroutines.withContext(Dispatchers.IO) { alertManagerApi.deleteSilence(ip, id) }

    // ── Logs ──────────────────────────────────────────────────────────────────

    suspend fun fetchLogs(ip: String, logql: String, limit: Int = 200): List<LogEntry> =
        kotlinx.coroutines.withContext(Dispatchers.IO) { lokiApi.queryRange(ip, logql, limit = limit) }

    fun tailLogs(ip: String, logql: String): Flow<LogEntry> = lokiApi.tailFlow(ip, logql)

    // ── W&B ───────────────────────────────────────────────────────────────────

    suspend fun getWandBRuns(entity: String, project: String, apiKey: String): List<WandBRun> =
        kotlinx.coroutines.withContext(Dispatchers.IO) { wandbApi.getRuns(entity, project, apiKey) }

    suspend fun getWandBHistory(
        entity: String, project: String, runId: String, apiKey: String
    ): List<WandBHistoryPoint> =
        kotlinx.coroutines.withContext(Dispatchers.IO) {
            wandbApi.getRunHistory(entity, project, runId, apiKey)
        }

    // ── PromQL constants ──────────────────────────────────────────────────────

    companion object {
        const val PI_CPU_QUERY     = "100 - (avg by(instance)(rate(node_cpu_seconds_total{mode=\"idle\",machine=\"raspberry_pi\"}[1m]))*100)"
        const val PI_RAM_QUERY     = "(1 - node_memory_MemAvailable_bytes{machine=\"raspberry_pi\"} / node_memory_MemTotal_bytes{machine=\"raspberry_pi\"}) * 100"
        const val PI_DISK_QUERY    = "(1 - node_filesystem_avail_bytes{machine=\"raspberry_pi\",mountpoint=\"/\"} / node_filesystem_size_bytes{machine=\"raspberry_pi\",mountpoint=\"/\"}) * 100"
        const val GPU_UTIL_QUERY   = "DCGM_FI_DEV_GPU_UTIL"
        const val GPU_VRAM_QUERY   = "DCGM_FI_DEV_MEM_USED / DCGM_FI_DEV_FB_TOTAL * 100"
        const val GPU_TEMP_QUERY   = "DCGM_FI_DEV_GPU_TEMP"
        const val ESTOP_QUERY      = "omnibot_estop_active"
        const val CONTROL_MODE_QUERY = "omnibot_control_mode"
        const val DRIVER_P50_QUERY = "omnibot_node_cycle_p50_ms{node=\"yahboom_controller_node\"}"
        const val DRIVER_P95_QUERY = "omnibot_node_cycle_p95_ms{node=\"yahboom_controller_node\"}"
        const val DRIVER_MAX_QUERY = "omnibot_node_cycle_max_ms{node=\"yahboom_controller_node\"}"
        const val VLA_MS_QUERY     = "omnibot_vla_inference_ms"
        const val RL_NAV_MS_QUERY  = "omnibot_rl_inference_ms{policy=\"nav\"}"
        const val RL_ARM_MS_QUERY  = "omnibot_rl_inference_ms{policy=\"arm\"}"
        const val ROBOT_VX_QUERY   = "omnibot_robot_vx_ms"
        const val ROBOT_VY_QUERY   = "omnibot_robot_vy_ms"
        const val ROBOT_OMEGA_QUERY= "omnibot_robot_omega_rads"
        const val MISSION_SUCCESS_QUERY = "rate(omnibot_missions_done_total{result=\"success\"}[5m]) / rate(omnibot_missions_done_total[5m])"
    }
}
