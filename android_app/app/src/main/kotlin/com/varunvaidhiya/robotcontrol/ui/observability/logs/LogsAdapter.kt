package com.varunvaidhiya.robotcontrol.ui.observability.logs

import android.graphics.Color
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.varunvaidhiya.robotcontrol.data.models.LogEntry
import com.varunvaidhiya.robotcontrol.databinding.ItemLogEntryBinding

class LogsAdapter : ListAdapter<LogEntry, LogsAdapter.VH>(DIFF) {

    inner class VH(val b: ItemLogEntryBinding) : RecyclerView.ViewHolder(b.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        VH(ItemLogEntryBinding.inflate(LayoutInflater.from(parent.context), parent, false))

    override fun onBindViewHolder(holder: VH, position: Int) {
        val entry = getItem(position)
        val b = holder.b

        val sevColor = when (entry.severity.uppercase()) {
            "ERROR"  -> Color.parseColor("#FF4444")
            "WARN"   -> Color.parseColor("#FFD700")
            "DEBUG"  -> Color.parseColor("#47" + "00E5FF".substring(2))
            else     -> Color.parseColor("#B8EEF8")
        }
        b.textTime.text = entry.displayTime
        b.textSeverity.text = entry.severity.take(4).uppercase()
        b.textSeverity.setTextColor(sevColor)
        b.textNode.text = entry.node.removePrefix("yahboom_").removePrefix("smolvla_").removePrefix("rl_")
        b.textMessage.text = entry.message
        b.accentDot.setBackgroundColor(sevColor)
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<LogEntry>() {
            override fun areItemsTheSame(a: LogEntry, b: LogEntry) =
                a.timestampNs == b.timestampNs && a.node == b.node
            override fun areContentsTheSame(a: LogEntry, b: LogEntry) = a == b
        }
    }
}
