package com.varunvaidhiya.robotcontrol.ui.observability.training

import android.graphics.Color
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.varunvaidhiya.robotcontrol.data.models.WandBRun
import com.varunvaidhiya.robotcontrol.databinding.ItemWandbRunBinding

class WandBRunAdapter(private val onSelect: (WandBRun) -> Unit) :
    ListAdapter<WandBRun, WandBRunAdapter.VH>(DIFF) {

    private var selectedId: String? = null

    inner class VH(val b: ItemWandbRunBinding) : RecyclerView.ViewHolder(b.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        VH(ItemWandbRunBinding.inflate(LayoutInflater.from(parent.context), parent, false))

    override fun onBindViewHolder(holder: VH, position: Int) {
        val run = getItem(position)
        val b = holder.b
        val isSelected = run.id == selectedId

        val stateColor = when (run.state.lowercase()) {
            "running"  -> Color.parseColor("#00FF7F")
            "finished" -> Color.parseColor("#00E5FF")
            "failed", "crashed" -> Color.parseColor("#FF4444")
            else -> Color.parseColor("#6100E5FF".substring(2).let { "#$it" })
        }

        b.textRunName.text = run.name
        b.textRunState.text = run.state.uppercase()
        b.textRunState.setTextColor(stateColor)
        b.textRunDate.text = run.createdAt.take(10)

        // Show summary loss or reward
        val summaryText = buildString {
            run.summary["train/loss"]?.let { append("loss=%.3f".format((it as? Double) ?: 0.0)) }
                ?: run.summary["Episode Reward/Mean"]?.let { append("rew=%.2f".format((it as? Double) ?: 0.0)) }
                ?: run.summary.entries.firstOrNull { it.value is Double }?.let {
                    append("${it.key.substringAfterLast("/")}=%.3f".format(it.value as Double))
                }
        }
        b.textRunSummary.text = summaryText

        b.accentBar.setBackgroundColor(stateColor)
        b.root.alpha = if (isSelected) 1.0f else 0.7f
        b.root.setOnClickListener {
            selectedId = run.id
            notifyDataSetChanged()
            onSelect(run)
        }
    }

    fun setSelected(id: String?) {
        selectedId = id
        notifyDataSetChanged()
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<WandBRun>() {
            override fun areItemsTheSame(a: WandBRun, b: WandBRun) = a.id == b.id
            override fun areContentsTheSame(a: WandBRun, b: WandBRun) = a == b
        }
    }
}
