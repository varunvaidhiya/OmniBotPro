package com.varunvaidhiya.robotcontrol.data.remote

import android.util.Base64
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.varunvaidhiya.robotcontrol.data.models.WandBHistoryPoint
import com.varunvaidhiya.robotcontrol.data.models.WandBRun
import com.varunvaidhiya.robotcontrol.data.models.WandBRunsResponse
import okhttp3.OkHttpClient
import okhttp3.Request
import javax.inject.Inject

class WandBApi @Inject constructor(
    private val okHttpClient: OkHttpClient,
    private val gson: Gson
) {
    private val baseUrl = "https://api.wandb.ai"

    private fun authHeader(apiKey: String): String {
        // W&B Basic auth: apiKey as username, empty password
        val encoded = Base64.encodeToString("$apiKey:".toByteArray(), Base64.NO_WRAP)
        return "Basic $encoded"
    }

    fun getRuns(entity: String, project: String, apiKey: String, perPage: Int = 10): List<WandBRun> =
        runCatching {
            val url = "$baseUrl/api/v1/runs/$entity/$project?per_page=$perPage&order=-created_at"
            val body = okHttpClient.newCall(
                Request.Builder().url(url).header("Authorization", authHeader(apiKey)).build()
            ).execute().use { it.body?.string() ?: "{}" }
            gson.fromJson(body, WandBRunsResponse::class.java)?.runs ?: emptyList()
        }.getOrDefault(emptyList())

    fun getRunHistory(
        entity: String,
        project: String,
        runId: String,
        apiKey: String,
        samples: Int = 150
    ): List<WandBHistoryPoint> = runCatching {
        val url = "$baseUrl/api/v1/runs/$entity/$project/$runId/history?samples=$samples"
        val body = okHttpClient.newCall(
            Request.Builder().url(url).header("Authorization", authHeader(apiKey)).build()
        ).execute().use { it.body?.string() ?: "[]" }

        val type = object : TypeToken<List<Map<String, Any?>>>() {}.type
        val raw: List<Map<String, Any?>> = gson.fromJson(body, type) ?: emptyList()
        raw.mapNotNull { row ->
            val step = (row["_step"] as? Double)?.toInt() ?: return@mapNotNull null
            val metrics = row.filterKeys { it != "_step" && it != "_timestamp" }
                .mapNotNull { (k, v) ->
                    val d = when (v) {
                        is Double -> v
                        is Long   -> v.toDouble()
                        is Int    -> v.toDouble()
                        else      -> null
                    } ?: return@mapNotNull null
                    k to d
                }.toMap()
            WandBHistoryPoint(step, metrics)
        }
    }.getOrDefault(emptyList())

    fun getRun(entity: String, project: String, runId: String, apiKey: String): WandBRun? =
        runCatching {
            val url = "$baseUrl/api/v1/runs/$entity/$project/$runId"
            val body = okHttpClient.newCall(
                Request.Builder().url(url).header("Authorization", authHeader(apiKey)).build()
            ).execute().use { it.body?.string() ?: "{}" }
            gson.fromJson(body, WandBRun::class.java)
        }.getOrNull()
}
