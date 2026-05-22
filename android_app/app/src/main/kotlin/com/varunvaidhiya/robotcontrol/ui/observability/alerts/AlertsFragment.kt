package com.varunvaidhiya.robotcontrol.ui.observability.alerts

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import com.varunvaidhiya.robotcontrol.databinding.FragmentObsAlertsBinding
import com.varunvaidhiya.robotcontrol.ui.observability.ObservabilityViewModel
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

class AlertsFragment : Fragment() {

    private var _binding: FragmentObsAlertsBinding? = null
    private val binding get() = _binding!!
    private val viewModel: ObservabilityViewModel by activityViewModels()
    private lateinit var adapter: AlertsAdapter

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentObsAlertsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        adapter = AlertsAdapter { alert ->
            viewLifecycleOwner.lifecycleScope.launch {
                viewModel.silenceAlert(viewModel.gpuIp.value, alert)
            }
        }
        binding.recyclerAlerts.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerAlerts.adapter = adapter

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    viewModel.alerts.collectLatest { alerts ->
                        val firing = alerts.filter { it.status.state == "active" || it.status.state == "suppressed" }
                        adapter.submitList(firing)
                        binding.textAlertCount.text = "${firing.size} FIRING"
                        binding.layoutEmpty.visibility  = if (firing.isEmpty()) View.VISIBLE else View.GONE
                        binding.recyclerAlerts.visibility = if (firing.isEmpty()) View.GONE else View.VISIBLE
                    }
                }
                launch {
                    viewModel.silenceResult.collect { msg ->
                        binding.textSilenceToast.text = msg
                        binding.textSilenceToast.visibility = View.VISIBLE
                        binding.textSilenceToast.postDelayed(
                            { binding.textSilenceToast.visibility = View.GONE }, 3_000L
                        )
                    }
                }
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
