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
import com.varunvaidhiya.robotcontrol.data.models.observability.LogEntry

class LogsAdapter : ListAdapter<LogEntry, LogsAdapter.VH>(DIFF) {

    inner class VH(view: View) : RecyclerView.ViewHolder(view) {
        val textTime: TextView = view.findViewById(R.id.log_time)
        val textLevel: TextView = view.findViewById(R.id.log_level)
        val textLine: TextView = view.findViewById(R.id.log_line)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        VH(LayoutInflater.from(parent.context).inflate(R.layout.item_log_entry, parent, false))

    override fun onBindViewHolder(holder: VH, position: Int) {
        val entry = getItem(position)
        holder.textTime.text = entry.displayTime
        val (lvlText, lvlColor) = when (entry.level) {
            "ERROR" -> "ERR" to Color.parseColor("#FF4444")
            "WARN"  -> "WRN" to Color.parseColor("#FFD700")
            "DEBUG" -> "DBG" to Color.parseColor("#6100E5FF")
            else    -> "INF" to Color.parseColor("#00E5FF")
        }
        holder.textLevel.text = lvlText
        holder.textLevel.setTextColor(lvlColor)
        holder.textLine.text = entry.line.take(120)
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<LogEntry>() {
            override fun areItemsTheSame(a: LogEntry, b: LogEntry) = a.timestampNs == b.timestampNs
            override fun areContentsTheSame(a: LogEntry, b: LogEntry) = a == b
        }
    }
}
