package com.varunvaidhiya.robotcontrol.ui.observability

import androidx.fragment.app.Fragment
import androidx.fragment.app.FragmentManager
import androidx.lifecycle.Lifecycle
import androidx.viewpager2.adapter.FragmentStateAdapter
import com.varunvaidhiya.robotcontrol.ui.observability.alerts.AlertsFragment
import com.varunvaidhiya.robotcontrol.ui.observability.health.HealthFragment
import com.varunvaidhiya.robotcontrol.ui.observability.logs.LogsFragment
import com.varunvaidhiya.robotcontrol.ui.observability.training.TrainingFragment

class ObservabilityPagerAdapter(fm: FragmentManager, lifecycle: Lifecycle) :
    FragmentStateAdapter(fm, lifecycle) {

    override fun getItemCount() = 4

    override fun createFragment(position: Int): Fragment = when (position) {
        0    -> HealthFragment()
        1    -> AlertsFragment()
        2    -> LogsFragment()
        3    -> TrainingFragment()
        else -> HealthFragment()
    }
}
