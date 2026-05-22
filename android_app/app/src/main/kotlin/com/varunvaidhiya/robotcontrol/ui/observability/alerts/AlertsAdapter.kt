package com.varunvaidhiya.robotcontrol.ui.observability.alerts

import android.graphics.Color
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.varunvaidhiya.robotcontrol.data.models.AmAlert
import com.varunvaidhiya.robotcontrol.databinding.ItemAlertBinding

class AlertsAdapter(private val onSilence: (AmAlert) -> Unit) :
    ListAdapter<AmAlert, AlertsAdapter.VH>(DIFF) {

    inner class VH(val b: ItemAlertBinding) : RecyclerView.ViewHolder(b.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        VH(ItemAlertBinding.inflate(LayoutInflater.from(parent.context), parent, false))

    override fun onBindViewHolder(holder: VH, position: Int) {
        val alert = getItem(position)
        val b = holder.b
        val severity = alert.labels["severity"] ?: "warning"
        val alertName = alert.labels["alertname"] ?: "Unknown"
        val isSilenced = alert.status.silencedBy.isNotEmpty() || alert.status.state == "suppressed"

        b.textAlertName.text = alertName
        b.textAlertSummary.text = alert.annotations["summary"] ?: alert.annotations["description"] ?: ""
        b.textAlertSince.text = "SINCE: ${formatTime(alert.startsAt)}"
        b.chipSeverity.text = severity.uppercase()

        val sevColor = when (severity.lowercase()) {
            "critical" -> Color.parseColor("#FF4444")
            "warning"  -> Color.parseColor("#FFD700")
            else       -> Color.parseColor("#00E5FF")
        }
        b.chipSeverity.setTextColor(sevColor)
        b.chipSeverity.setStrokeColor(android.content.res.ColorStateList.valueOf(sevColor))

        b.btnSilence.text = if (isSilenced) "SILENCED" else "SILENCE 4H"
        b.btnSilence.alpha = if (isSilenced) 0.5f else 1.0f
        b.btnSilence.isEnabled = !isSilenced
        b.btnSilence.setOnClickListener { onSilence(alert) }

        b.accentBar.setBackgroundColor(sevColor)
    }

    private fun formatTime(iso: String): String = runCatching {
        val t = java.time.Instant.parse(iso)
        val ldt = java.time.LocalDateTime.ofInstant(t, java.time.ZoneId.systemDefault())
        "%02d:%02d".format(ldt.hour, ldt.minute)
    }.getOrDefault(iso.take(16))

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<AmAlert>() {
            override fun areItemsTheSame(a: AmAlert, b: AmAlert) = a.fingerprint == b.fingerprint
            override fun areContentsTheSame(a: AmAlert, b: AmAlert) = a == b
        }
    }
}
