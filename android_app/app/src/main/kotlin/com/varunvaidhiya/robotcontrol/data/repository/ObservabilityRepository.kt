package com.varunvaidhiya.robotcontrol.data.repository

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.varunvaidhiya.robotcontrol.data.models.observability.*
import com.varunvaidhiya.robotcontrol.data.preferences.AppPreferences
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import timber.log.Timber
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ObservabilityRepository @Inject constructor(
    private val preferences: AppPreferences
) {
    private val gson = Gson()
    private val client = OkHttpClient.Builder()
        .connectTimeout(5, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .build()

    // ── Prometheus ────────────────────────────────────────────────────────────

    suspend fun fetchRobotHealth(prometheusBase: String): Result<RobotHealthData> =
        withContext(Dispatchers.IO) {
            try {
                val queries = mapOf(
                    "estop"       to "omnibot_estop_active",
                    "mode"        to "omnibot_control_mode_info",
                    "vx"          to "omnibot_robot_vx",
                    "vy"          to "omnibot_robot_vy",
                    "omega"       to "omnibot_robot_omega",
                    "cycle_p95"   to "omnibot_node_cycle_p95_ms{node=\"yahboom_controller_node\"}",
                    "missions"    to "sum(omnibot_missions_total)",
                    "missions_ok" to "sum(omnibot_missions_done_total{result=\"success\"})",
                    "vla_ms"      to "omnibot_vla_inference_ms"
                )
                val results = mutableMapOf<String, Double>()
                var controlMode = "--"
                for ((key, query) in queries) {
                    val v = queryPrometheus(prometheusBase, query)
                    if (key == "mode") {
                        // mode metric has a label "mode"
                        controlMode = getPrometheusLabel(prometheusBase, query, "mode") ?: "--"
                    } else {
                        results[key] = v
                    }
                }
                Result.success(RobotHealthData(
                    estopActive = results["estop"] != 0.0,
                    controlMode = controlMode,
                    vx = results["vx"] ?: 0.0,
                    vy = results["vy"] ?: 0.0,
                    omega = results["omega"] ?: 0.0,
                    driverCycleP95Ms = results["cycle_p95"] ?: 0.0,
                    missionsTotal = results["missions"] ?: 0.0,
                    missionsDoneOk = results["missions_ok"] ?: 0.0,
                    vlaInferenceMs = results["vla_ms"] ?: 0.0
                ))
            } catch (e: Exception) {
                Timber.w(e, "fetchRobotHealth failed")
                Result.failure(e)
            }
        }

    private fun queryPrometheus(base: String, query: String): Double {
        val url = "$base/api/v1/query?query=${java.net.URLEncoder.encode(query, "UTF-8")}"
        val resp = client.newCall(Request.Builder().url(url).build()).execute()
        val body = resp.body?.string() ?: return 0.0
        val pr = gson.fromJson(body, PrometheusResult::class.java)
        val vec = pr.data?.result?.firstOrNull() ?: return 0.0
        val valStr = (vec.value.getOrNull(1) as? String) ?: return 0.0
        return valStr.toDoubleOrNull() ?: 0.0
    }

    private fun getPrometheusLabel(base: String, query: String, label: String): String? {
        val url = "$base/api/v1/query?query=${java.net.URLEncoder.encode(query, "UTF-8")}"
        val resp = client.newCall(Request.Builder().url(url).build()).execute()
        val body = resp.body?.string() ?: return null
        val pr = gson.fromJson(body, PrometheusResult::class.java)
        return pr.data?.result?.firstOrNull { it.metric[label] != null }?.metric?.get(label)
    }

    // ── AlertManager ─────────────────────────────────────────────────────────

    suspend fun fetchAlerts(alertManagerBase: String): Result<List<AlertManagerAlert>> =
        withContext(Dispatchers.IO) {
            try {
                val url = "$alertManagerBase/api/v2/alerts"
                val resp = client.newCall(Request.Builder().url(url).build()).execute()
                val body = resp.body?.string() ?: "[]"
                val type = object : TypeToken<List<AlertManagerAlert>>() {}.type
                val alerts: List<AlertManagerAlert> = gson.fromJson(body, type)
                Result.success(alerts)
            } catch (e: Exception) {
                Timber.w(e, "fetchAlerts failed")
                Result.failure(e)
            }
        }

    // ── Loki ─────────────────────────────────────────────────────────────────

    suspend fun fetchLogs(lokiBase: String, limit: Int = 50): Result<List<LogEntry>> =
        withContext(Dispatchers.IO) {
            try {
                val query = java.net.URLEncoder.encode("{job=\"ros2\"}", "UTF-8")
                val end = System.currentTimeMillis() * 1_000_000L
                val start = end - 10L * 60 * 1_000_000_000L // last 10 min
                val url = "$lokiBase/loki/api/v1/query_range?query=$query&limit=$limit&start=$start&end=$end&direction=backward"
                val resp = client.newCall(Request.Builder().url(url).build()).execute()
                val body = resp.body?.string() ?: return@withContext Result.success(emptyList())
                val loki = gson.fromJson(body, LokiQueryResult::class.java)
                val entries = loki.data?.result?.flatMap { stream ->
                    stream.values.map { pair ->
                        val ts = pair[0].toLongOrNull() ?: 0L
                        val line = pair[1]
                        val level = when {
                            line.contains("[ERROR]", ignoreCase = true) || line.contains("error", ignoreCase = true) -> "ERROR"
                            line.contains("[WARN]", ignoreCase = true)  || line.contains("warn", ignoreCase = true)  -> "WARN"
                            line.contains("[DEBUG]", ignoreCase = true) -> "DEBUG"
                            else -> "INFO"
                        }
                        LogEntry(ts, line, level)
                    }
                }?.sortedByDescending { it.timestampNs } ?: emptyList()
                Result.success(entries)
            } catch (e: Exception) {
                Timber.w(e, "fetchLogs failed")
                Result.failure(e)
            }
        }

    // ── Weights & Biases ──────────────────────────────────────────────────────

    suspend fun fetchWandBRuns(entity: String, project: String, apiKey: String): Result<List<WandBRun>> =
        withContext(Dispatchers.IO) {
            try {
                if (apiKey.isBlank() || entity.isBlank() || project.isBlank()) {
                    return@withContext Result.success(emptyList())
                }
                val url = "https://api.wandb.ai/api/v1/runs/$entity/$project?per_page=10&order=-created_at"
                val req = Request.Builder()
                    .url(url)
                    .header("Authorization", "Bearer $apiKey")
                    .build()
                val resp = client.newCall(req).execute()
                if (!resp.isSuccessful) return@withContext Result.success(emptyList())
                val body = resp.body?.string() ?: return@withContext Result.success(emptyList())
                // W&B REST returns {"runs": [...]}
                val type = object : TypeToken<Map<String, Any>>() {}.type
                val map: Map<String, Any> = gson.fromJson(body, type)
                @Suppress("UNCHECKED_CAST")
                val rawRuns = (map["runs"] as? List<Map<String, Any>>) ?: emptyList()
                val runs = rawRuns.map { r ->
                    WandBRun(
                        id = r["id"] as? String ?: "",
                        name = r["name"] as? String ?: r["id"] as? String ?: "",
                        state = r["state"] as? String ?: "",
                        createdAt = r["createdAt"] as? String ?: ""
                    )
                }
                Result.success(runs)
            } catch (e: Exception) {
                Timber.w(e, "fetchWandBRuns failed")
                Result.failure(e)
            }
        }
}
