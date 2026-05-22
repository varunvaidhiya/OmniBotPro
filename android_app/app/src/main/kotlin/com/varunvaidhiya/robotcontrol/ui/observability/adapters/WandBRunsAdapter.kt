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
import com.varunvaidhiya.robotcontrol.data.models.observability.WandBRun

class WandBRunsAdapter : ListAdapter<WandBRun, WandBRunsAdapter.VH>(DIFF) {

    inner class VH(view: View) : RecyclerView.ViewHolder(view) {
        val textName: TextView = view.findViewById(R.id.run_name)
        val textState: TextView = view.findViewById(R.id.run_state)
        val textId: TextView = view.findViewById(R.id.run_id)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        VH(LayoutInflater.from(parent.context).inflate(R.layout.item_wandb_run, parent, false))

    override fun onBindViewHolder(holder: VH, position: Int) {
        val run = getItem(position)
        holder.textName.text = run.name.take(24)
        holder.textId.text = run.id.take(8)
        val (stateText, stateColor) = when (run.state.lowercase()) {
            "running"  -> "● RUNNING"  to Color.parseColor("#00FF7F")
            "finished" -> "✓ DONE"     to Color.parseColor("#00E5FF")
            "crashed"  -> "✗ CRASHED"  to Color.parseColor("#FF4444")
            "failed"   -> "✗ FAILED"   to Color.parseColor("#FF4444")
            else       -> run.state.uppercase() to Color.parseColor("#6100E5FF")
        }
        holder.textState.text = stateText
        holder.textState.setTextColor(stateColor)
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<WandBRun>() {
            override fun areItemsTheSame(a: WandBRun, b: WandBRun) = a.id == b.id
            override fun areContentsTheSame(a: WandBRun, b: WandBRun) = a == b
        }
    }
}
