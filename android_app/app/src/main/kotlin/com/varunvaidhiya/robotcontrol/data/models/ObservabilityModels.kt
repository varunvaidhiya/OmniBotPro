package com.varunvaidhiya.robotcontrol.data.models

// ── Prometheus ────────────────────────────────────────────────────────────────

data class PrometheusResult(
    val status: String,
    val data: PrometheusData?
)

data class PrometheusData(
    val resultType: String,
    val result: List<PrometheusVector>
)

data class PrometheusVector(
    val metric: Map<String, String>,
    val value: List<Any>?,       // instant: [timestamp, "strValue"]
    val values: List<List<Any>>? // range:   [[ts, "v"], ...]
)

data class PrometheusAlertsResult(
    val status: String,
    val data: PrometheusAlertsData?
)

data class PrometheusAlertsData(val alerts: List<PrometheusAlert>)

data class PrometheusAlert(
    val labels: Map<String, String>,
    val annotations: Map<String, String>,
    val state: String,
    val activeAt: String,
    val value: String
)

data class PrometheusTargetsResult(
    val status: String,
    val data: PrometheusTargetsData?
)

data class PrometheusTargetsData(val activeTargets: List<PrometheusTarget>)

data class PrometheusTarget(
    val labels: Map<String, String>,
    val scrapeUrl: String,
    val health: String,   // "up" | "down" | "unknown"
    val lastError: String?
)

// ── Aggregated health snapshot consumed by HealthFragment ─────────────────────

data class RobotHealthSnapshot(
    val timestamp: Long = System.currentTimeMillis(),
    // System resources
    val piCpuPct: Double = 0.0,
    val piRamPct: Double = 0.0,
    val piDiskPct: Double = 0.0,
    val gpuUtilPct: Double = 0.0,
    val gpuVramPct: Double = 0.0,
    val gpuTempC: Double = 0.0,
    // Control
    val estopActive: Boolean = false,
    val controlMode: String = "--",
    // Node timing
    val driverP50ms: Double = 0.0,
    val driverP95ms: Double = 0.0,
    val driverMaxMs: Double = 0.0,
    val vlaInferenceMs: Double = 0.0,
    val rlNavMs: Double = 0.0,
    val rlArmMs: Double = 0.0,
    // Robot state
    val robotVx: Double = 0.0,
    val robotVy: Double = 0.0,
    val robotOmega: Double = 0.0,
    // Missions
    val missionSuccessRate: Double = -1.0,
    // Targets
    val targets: List<TargetStatus> = emptyList(),
    // Firing alerts summary
    val firingCritical: Int = 0,
    val firingWarning: Int = 0,
    val error: String? = null
)

data class TargetStatus(val job: String, val health: String)

// ── AlertManager ──────────────────────────────────────────────────────────────

data class AmAlert(
    val labels: Map<String, String>,
    val annotations: Map<String, String>,
    val status: AmAlertStatus,
    val startsAt: String,
    val endsAt: String,
    val fingerprint: String
)

data class AmAlertStatus(
    val state: String,         // "active" | "suppressed"
    val silencedBy: List<String>,
    val inhibitedBy: List<String>
)

data class AmSilence(
    val id: String,
    val matchers: List<AmMatcher>,
    val startsAt: String,
    val endsAt: String,
    val createdBy: String,
    val comment: String,
    val status: AmSilenceStatus
)

data class AmSilenceStatus(val state: String) // "active" | "expired"

data class AmMatcher(val name: String, val value: String, val isRegex: Boolean)

data class CreateSilenceRequest(
    val matchers: List<AmMatcher>,
    val startsAt: String,
    val endsAt: String,
    val createdBy: String,
    val comment: String
)

// ── Loki ──────────────────────────────────────────────────────────────────────

data class LokiQueryResult(
    val status: String,
    val data: LokiData?
)

data class LokiData(
    val resultType: String,
    val result: List<LokiStream>
)

data class LokiStream(
    val stream: Map<String, String>,
    val values: List<List<String>>  // [[ns_timestamp, log_line], ...]
)

data class LogEntry(
    val timestampNs: Long,
    val machine: String,
    val node: String,
    val severity: String,
    val message: String
) {
    val displayTime: String get() {
        val ms = timestampNs / 1_000_000L
        val s = ms / 1000L
        val h = (s / 3600) % 24
        val m = (s / 60) % 60
        val sec = s % 60
        return "%02d:%02d:%02d".format(h, m, sec)
    }
}

// ── W&B ───────────────────────────────────────────────────────────────────────

data class WandBRunsResponse(
    val runs: List<WandBRun>,
    val total: Int
)

data class WandBRun(
    val id: String,
    val name: String,
    val state: String,          // "running" | "finished" | "failed" | "crashed"
    val createdAt: String,
    val heartbeatAt: String?,
    val config: Map<String, Any?>,
    val summary: Map<String, Any?>
)

data class WandBHistoryPoint(
    val step: Int,
    val metrics: Map<String, Double>
)

// Which W&B project is selected in the Training tab
enum class WandBProject(val displayName: String, val projectId: String, val chartKeys: List<String>) {
    SMOLVLA("SmolVLA", "omnibot_smolvla", listOf("train/loss", "eval/loss", "grad_norm")),
    RL_NAV("RL NAV",   "omnibot_nav",     listOf("Episode Reward/Mean", "Episode Length/Mean", "Loss/Value")),
    RL_ARM("RL ARM",   "omnibot_arm",     listOf("Episode Reward/Mean", "Episode Length/Mean", "Loss/Value"))
}
