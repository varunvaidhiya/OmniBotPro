package com.varunvaidhiya.robotcontrol.ui.observability

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.viewpager2.widget.ViewPager2
import com.varunvaidhiya.robotcontrol.R
import com.varunvaidhiya.robotcontrol.databinding.FragmentObservabilityBinding
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class ObservabilityFragment : Fragment() {

    private var _binding: FragmentObservabilityBinding? = null
    private val binding get() = _binding!!

    // ViewModel is shared by child fragments via activityViewModels()
    private val viewModel: ObservabilityViewModel by viewModels()

    private val colorCyan = Color.parseColor("#00E5FF")
    private val colorMuted = Color.parseColor("#6100E5FF")

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentObservabilityBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val adapter = ObservabilityPagerAdapter(childFragmentManager, viewLifecycleOwner.lifecycle)
        binding.viewPager.adapter = adapter
        binding.viewPager.isUserInputEnabled = false   // tab buttons drive navigation

        // Sync tab buttons → ViewPager2
        binding.tabGroup.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (!isChecked) return@addOnButtonCheckedListener
            val page = when (checkedId) {
                R.id.tab_health   -> 0
                R.id.tab_alerts   -> 1
                R.id.tab_logs     -> 2
                R.id.tab_training -> 3
                else -> 0
            }
            binding.viewPager.setCurrentItem(page, false)
            updateTabColors(checkedId)
        }

        // Sync ViewPager2 → tab buttons (if ever driven externally)
        binding.viewPager.registerOnPageChangeCallback(object : ViewPager2.OnPageChangeCallback() {
            override fun onPageSelected(position: Int) {
                val tabId = when (position) {
                    0 -> R.id.tab_health
                    1 -> R.id.tab_alerts
                    2 -> R.id.tab_logs
                    3 -> R.id.tab_training
                    else -> R.id.tab_health
                }
                binding.tabGroup.check(tabId)
            }
        })

        // Select HEALTH by default
        binding.tabGroup.check(R.id.tab_health)
    }

    private fun updateTabColors(selectedId: Int) {
        listOf(
            binding.tabHealth   to R.id.tab_health,
            binding.tabAlerts   to R.id.tab_alerts,
            binding.tabLogs     to R.id.tab_logs,
            binding.tabTraining to R.id.tab_training
        ).forEach { (btn, id) ->
            btn.setTextColor(if (id == selectedId) colorCyan else colorMuted)
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
