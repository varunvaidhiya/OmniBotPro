package com.varunvaidhiya.robotcontrol.data.models.observability

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
    val value: List<Any> // [timestamp, "value_string"]
)

data class RobotHealthData(
    val estopActive: Boolean = false,
    val controlMode: String = "--",
    val vx: Double = 0.0,
    val vy: Double = 0.0,
    val omega: Double = 0.0,
    val driverCycleP95Ms: Double = 0.0,
    val missionsTotal: Double = 0.0,
    val missionsDoneOk: Double = 0.0,
    val vlaInferenceMs: Double = 0.0
)

data class AlertManagerAlert(
    val labels: Map<String, String> = emptyMap(),
    val annotations: Map<String, String> = emptyMap(),
    val state: String = "",
    val startsAt: String = ""
) {
    val alertName: String get() = labels["alertname"] ?: "Unknown"
    val severity: String get() = labels["severity"] ?: "info"
    val summary: String get() = annotations["summary"] ?: annotations["message"] ?: ""
}

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
    val values: List<List<String>> // [[timestamp_ns_str, log_line], ...]
)

data class LogEntry(
    val timestampNs: Long,
    val line: String,
    val level: String // "ERROR", "WARN", "INFO", "DEBUG"
) {
    val displayTime: String get() {
        val seconds = timestampNs / 1_000_000_000L
        val h = (seconds % 86400) / 3600
        val m = (seconds % 3600) / 60
        val s = seconds % 60
        return "%02d:%02d:%02d".format(h, m, s)
    }
}

data class WandBRun(
    val id: String = "",
    val name: String = "",
    val state: String = "",
    val createdAt: String = "",
    val summaryMetrics: Map<String, Double> = emptyMap()
)

data class WandBRunsResponse(
    val runs: List<WandBRun> = emptyList()
)
