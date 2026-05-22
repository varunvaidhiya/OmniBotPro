package com.varunvaidhiya.robotcontrol.data.remote

import com.google.gson.Gson
import com.varunvaidhiya.robotcontrol.data.models.LogEntry
import com.varunvaidhiya.robotcontrol.data.models.LokiQueryResult
import kotlinx.coroutines.channels.awaitClose
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.callbackFlow
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import javax.inject.Inject

class LokiApi @Inject constructor(
    private val okHttpClient: OkHttpClient,
    private val gson: Gson
) {
    private fun baseUrl(ip: String) = "http://$ip:3100"

    /** Fetch the last [limit] log lines matching [logql] as a one-shot HTTP call. */
    fun queryRange(
        ip: String,
        logql: String,
        startNs: Long = System.currentTimeMillis() * 1_000_000L - 5 * 60 * 1_000_000_000L,
        endNs: Long   = System.currentTimeMillis() * 1_000_000L,
        limit: Int    = 200
    ): List<LogEntry> = runCatching {
        val encoded = java.net.URLEncoder.encode(logql, "UTF-8")
        val url = "${baseUrl(ip)}/loki/api/v1/query_range" +
                "?query=$encoded&start=$startNs&end=$endNs&limit=$limit&direction=backward"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "" }
        parseLokiResult(gson.fromJson(body, LokiQueryResult::class.java))
    }.getOrDefault(emptyList())

    /**
     * Opens a WebSocket to the Loki tail endpoint and emits log entries as they arrive.
     * The Flow stays open until the caller cancels it.
     */
    fun tailFlow(ip: String, logql: String): Flow<LogEntry> = callbackFlow {
        val encoded = java.net.URLEncoder.encode(logql, "UTF-8")
        val wsUrl = "ws://$ip:3100/loki/api/v1/tail?query=$encoded&limit=20"
        val ws = okHttpClient.newWebSocket(
            Request.Builder().url(wsUrl).build(),
            object : WebSocketListener() {
                override fun onMessage(webSocket: WebSocket, text: String) {
                    parseLokiTailMessage(text).forEach { trySend(it) }
                }
                override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
                    close(t)
                }
            }
        )
        awaitClose { ws.close(1000, "Flow cancelled") }
    }

    private fun parseLokiResult(result: LokiQueryResult): List<LogEntry> {
        val entries = mutableListOf<LogEntry>()
        result.data?.result?.forEach { stream ->
            stream.values.forEach { (tsStr, line) ->
                entries += logEntryFrom(stream.stream, tsStr.toLongOrNull() ?: 0L, line)
            }
        }
        return entries.sortedByDescending { it.timestampNs }
    }

    private fun parseLokiTailMessage(text: String): List<LogEntry> = runCatching {
        // Tail response: {"streams": [{"stream": {...}, "values": [[ts, line], ...]}]}
        val map = gson.fromJson(text, Map::class.java)
        @Suppress("UNCHECKED_CAST")
        val streams = map["streams"] as? List<Map<*, *>> ?: return@runCatching emptyList()
        val entries = mutableListOf<LogEntry>()
        streams.forEach { s ->
            @Suppress("UNCHECKED_CAST")
            val labels  = s["stream"]  as? Map<String, String> ?: emptyMap()
            @Suppress("UNCHECKED_CAST")
            val values  = s["values"]  as? List<List<*>> ?: emptyList()
            values.forEach { pair ->
                val ts   = (pair.getOrNull(0) as? String)?.toLongOrNull() ?: 0L
                val line = pair.getOrNull(1) as? String ?: ""
                entries += logEntryFrom(labels, ts, line)
            }
        }
        entries
    }.getOrDefault(emptyList())

    private fun logEntryFrom(labels: Map<String, String>, ts: Long, line: String) = LogEntry(
        timestampNs = ts,
        machine     = labels["machine"] ?: labels["host"] ?: "unknown",
        node        = labels["node"]    ?: "unknown",
        severity    = labels["severity"] ?: extractSeverityFromLine(line),
        message     = line
    )

    private fun extractSeverityFromLine(line: String): String {
        return when {
            line.contains("[ERROR]") || line.contains("ERROR") -> "ERROR"
            line.contains("[WARN]")  || line.contains("WARN")  -> "WARN"
            line.contains("[INFO]")  || line.contains("INFO")  -> "INFO"
            line.contains("[DEBUG]") || line.contains("DEBUG") -> "DEBUG"
            else -> "INFO"
        }
    }
}
