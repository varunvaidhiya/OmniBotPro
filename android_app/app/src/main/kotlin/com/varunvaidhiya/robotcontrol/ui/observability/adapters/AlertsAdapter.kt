package com.varunvaidhiya.robotcontrol.ui.observability.adapters

import android.graphics.Color
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.data.models.observability.AlertManagerAlert

class AlertsAdapter : ListAdapter<AlertManagerAlert, AlertsAdapter.VH>(DIFF) {

    inner class VH(view: View) : RecyclerView.ViewHolder(view) {
        val textName: TextView = view.findViewById(R.id.alert_name)
        val textSeverity: TextView = view.findViewById(R.id.alert_severity)
        val textSummary: TextView = view.findViewById(R.id.alert_summary)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        VH(LayoutInflater.from(parent.context).inflate(R.layout.item_alert, parent, false))

    override fun onBindViewHolder(holder: VH, position: Int) {
        val alert = getItem(position)
        holder.textName.text = alert.alertName
        val (sevText, sevColor) = when (alert.severity.lowercase()) {
            "critical" -> "CRITICAL" to Color.parseColor("#FF4444")
            "warning"  -> "WARNING"  to Color.parseColor("#FFD700")
            else       -> "INFO"     to Color.parseColor("#00E5FF")
        }
        holder.textSeverity.text = sevText
        holder.textSeverity.setTextColor(sevColor)
        holder.textSummary.text = alert.summary.ifBlank { alert.alertName }
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<AlertManagerAlert>() {
            override fun areItemsTheSame(a: AlertManagerAlert, b: AlertManagerAlert) =
                a.alertName == b.alertName && a.startsAt == b.startsAt
            override fun areContentsTheSame(a: AlertManagerAlert, b: AlertManagerAlert) = a == b
        }
    }
}
