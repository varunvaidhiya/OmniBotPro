package com.varunvaidhiya.robotcontrol.data.remote

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.varunvaidhiya.robotcontrol.data.models.PrometheusAlertsResult
import com.varunvaidhiya.robotcontrol.data.models.PrometheusResult
import com.varunvaidhiya.robotcontrol.data.models.PrometheusTargetsResult
import okhttp3.OkHttpClient
import okhttp3.Request
import javax.inject.Inject
import javax.inject.Named

class PrometheusApi @Inject constructor(
    private val okHttpClient: OkHttpClient,
    private val gson: Gson
) {
    private fun baseUrl(ip: String) = "http://$ip:9090"

    fun queryInstant(ip: String, promql: String): PrometheusResult {
        val encoded = java.net.URLEncoder.encode(promql, "UTF-8")
        val url = "${baseUrl(ip)}/api/v1/query?query=$encoded"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "" }
        return gson.fromJson(body, PrometheusResult::class.java)
    }

    fun queryRange(ip: String, promql: String, startSec: Long, endSec: Long, step: String = "5s"): PrometheusResult {
        val encoded = java.net.URLEncoder.encode(promql, "UTF-8")
        val url = "${baseUrl(ip)}/api/v1/query_range?query=$encoded&start=$startSec&end=$endSec&step=$step"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "" }
        return gson.fromJson(body, PrometheusResult::class.java)
    }

    fun getAlerts(ip: String): PrometheusAlertsResult {
        val url = "${baseUrl(ip)}/api/v1/alerts"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "" }
        return gson.fromJson(body, PrometheusAlertsResult::class.java)
    }

    fun getTargets(ip: String): PrometheusTargetsResult {
        val url = "${baseUrl(ip)}/api/v1/targets"
        val body = okHttpClient.newCall(Request.Builder().url(url).build()).execute()
            .use { it.body?.string() ?: "" }
        return gson.fromJson(body, PrometheusTargetsResult::class.java)
    }

    /** Returns the scalar double value from the first result vector, or null on any error. */
    fun scalarOrNull(ip: String, promql: String): Double? = runCatching {
        val result = queryInstant(ip, promql)
        val vec = result.data?.result?.firstOrNull() ?: return@runCatching null
        (vec.value?.getOrNull(1) as? String)?.toDoubleOrNull()
    }.getOrNull()

    /** Returns all labeled values from an instant query, as map of label→value. */
    fun labeledScalars(ip: String, promql: String, labelKey: String): Map<String, Double> {
        return runCatching {
            val result = queryInstant(ip, promql)
            buildMap {
                result.data?.result?.forEach { vec ->
                    val label = vec.metric[labelKey] ?: return@forEach
                    val v = (vec.value?.getOrNull(1) as? String)?.toDoubleOrNull() ?: return@forEach
                    put(label, v)
                }
            }
        }.getOrDefault(emptyMap())
    }
}
