package com.varunvaidhiya.robotcontrol.data.remote

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.varunvaidhiya.robotcontrol.data.models.AmAlert
import com.varunvaidhiya.robotcontrol.data.models.AmSilence
import com.varunvaidhiya.robotcontrol.data.models.CreateSilenceRequest
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import javax.inject.Inject

class AlertManagerApi @Inject constructor(
    private val okHttpClient: OkHttpClient,
    private val gson: Gson
) {
    private fun baseUrl(ip: String) = "http://$ip:9093"

    fun getAlerts(ip: String): List<AmAlert> = runCatching {
        val url = "${baseUrl(ip)}/api/v2/alerts"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "[]" }
        val type = object : TypeToken<List<AmAlert>>() {}.type
        gson.fromJson<List<AmAlert>>(body, type) ?: emptyList()
    }.getOrDefault(emptyList())

    fun getSilences(ip: String): List<AmSilence> = runCatching {
        val url = "${baseUrl(ip)}/api/v2/silences"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "[]" }
        val type = object : TypeToken<List<AmSilence>>() {}.type
        gson.fromJson<List<AmSilence>>(body, type) ?: emptyList()
    }.getOrDefault(emptyList())

    fun createSilence(ip: String, req: CreateSilenceRequest): String? = runCatching {
        val json = gson.toJson(req)
        val reqBody = json.toRequestBody("application/json".toMediaType())
        val request = Request.Builder()
            .url("${baseUrl(ip)}/api/v2/silences")
            .post(reqBody)
            .build()
        val body = okHttpClient.newCall(request).execute().use { it.body?.string() ?: "" }
        // Response: {"silenceID": "..."}
        gson.fromJson(body, Map::class.java)?.get("silenceID") as? String
    }.getOrNull()

    fun deleteSilence(ip: String, silenceId: String): Boolean = runCatching {
        val request = Request.Builder()
            .url("${baseUrl(ip)}/api/v2/silences/$silenceId")
            .delete()
            .build()
        okHttpClient.newCall(request).execute().use { it.isSuccessful }
    }.getOrDefault(false)
}
