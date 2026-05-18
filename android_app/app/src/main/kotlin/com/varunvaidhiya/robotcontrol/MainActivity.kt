package com.varunvaidhiya.robotcontrol

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.app.AppCompatDelegate
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.navigation.fragment.NavHostFragment
import androidx.navigation.ui.setupWithNavController
import com.varunvaidhiya.robotcontrol.databinding.ActivityMainBinding
import dagger.hilt.android.AndroidEntryPoint
import timber.log.Timber
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@AndroidEntryPoint
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private val timeHandler = Handler(Looper.getMainLooper())
    private val timeFmt = SimpleDateFormat("HH:mm", Locale.US)

    override fun onCreate(savedInstanceState: Bundle?) {
        AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES)
        super.onCreate(savedInstanceState)

        // Go edge-to-edge: we will manually pad our views to avoid system bars
        WindowCompat.setDecorFitsSystemWindows(window, false)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Apply system bar insets precisely to our two edge views
        ViewCompat.setOnApplyWindowInsetsListener(binding.root) { _, insets ->
            val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars())

            // Pad the HUD bar so it starts below the system status bar
            binding.hudStatusBar.apply {
                setPadding(paddingLeft, bars.top, paddingRight, paddingBottom)
            }

            // Pad the bottom nav so it doesn't go under the system nav bar
            binding.navView.apply {
                setPadding(paddingLeft, paddingTop, paddingRight, bars.bottom)
            }

            insets
        }

        val navHostFragment = supportFragmentManager
            .findFragmentById(R.id.nav_host_fragment) as? NavHostFragment
        val navController = navHostFragment?.navController

        if (navController != null) {
            binding.navView.setupWithNavController(navController)
            navController.addOnDestinationChangedListener { _, destination, _ ->
                val title = when (destination.id) {
                    R.id.navigation_home      -> "HOME SCREEN"
                    R.id.navigation_dashboard -> "DASHBOARD"
                    R.id.navigation_map       -> "SLAM MAP"
                    R.id.navigation_ai        -> "AI INTERFACE"
                    R.id.navigation_controls  -> "ROBOT CONTROLS"
                    R.id.navigation_settings  -> "SETTINGS"
                    else -> destination.label?.toString() ?: ""
                }
                binding.textScreenTitle.text = title
            }

            binding.btnSettings.setOnClickListener {
                navController.navigate(R.id.navigation_settings)
            }
        }

        updateTime()
        Timber.i("MainActivity initialized")
    }

    private fun updateTime() {
        if (!isFinishing && !isDestroyed) {
            binding.textStatusTime.text = timeFmt.format(Date())
            timeHandler.postDelayed(::updateTime, 30_000L)
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        timeHandler.removeCallbacksAndMessages(null)
    }
}
