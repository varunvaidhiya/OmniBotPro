package com.varunvaidhiya.robotcontrol

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.app.AppCompatDelegate
import androidx.navigation.findNavController
import androidx.navigation.ui.setupWithNavController
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.varunvaidhiya.robotcontrol.databinding.ActivityMainBinding
import com.varunvaidhiya.robotcontrol.ui.MainViewModel
import dagger.hilt.android.AndroidEntryPoint
import timber.log.Timber
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@AndroidEntryPoint
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private val mainViewModel: MainViewModel by viewModels()
    private val timeHandler = Handler(Looper.getMainLooper())
    private val timeFmt = SimpleDateFormat("HH:mm", Locale.US)

    private val screenLabels = mapOf(
        R.id.navigation_home      to "HOME SCREEN",
        R.id.navigation_dashboard to "DASHBOARD",
        R.id.navigation_map       to "SLAM MAP",
        R.id.navigation_ai        to "AI INTERFACE",
        R.id.navigation_controls  to "ROBOT CONTROLS",
        R.id.navigation_settings  to "SETTINGS"
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES)
        super.onCreate(savedInstanceState)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        if (BuildConfig.DEBUG) {
            Timber.plant(Timber.DebugTree())
        }

        val navView: BottomNavigationView = binding.navView
        val navController = findNavController(R.id.nav_host_fragment)

        navView.setupWithNavController(navController)

        navController.addOnDestinationChangedListener { _, destination, _ ->
            binding.textScreenTitle.text = screenLabels[destination.id] ?: destination.label
        }

        startClock()
        Timber.i("MainActivity initialized")
    }

    private fun startClock() {
        val tick = object : Runnable {
            override fun run() {
                binding.textStatusTime.text = timeFmt.format(Date())
                timeHandler.postDelayed(this, 30_000L)
            }
        }
        tick.run()
    }

    override fun onDestroy() {
        super.onDestroy()
        timeHandler.removeCallbacksAndMessages(null)
    }
}
