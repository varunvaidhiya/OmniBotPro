package com.varunvaidhiya.robotcontrol.data.preferences

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.varunvaidhiya.robotcontrol.utils.Constants
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject

val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "robot_settings")

class AppPreferences @Inject constructor(@ApplicationContext private val context: Context) {

    private val KEY_ROBOT_IP = stringPreferencesKey("robot_ip")
    private val KEY_ROBOT_PORT = intPreferencesKey("robot_port")
    private val KEY_PROMETHEUS_BASE   = stringPreferencesKey("prometheus_base")
    private val KEY_ALERTMANAGER_BASE = stringPreferencesKey("alertmanager_base")
    private val KEY_LOKI_BASE         = stringPreferencesKey("loki_base")
    private val KEY_WANDB_ENTITY      = stringPreferencesKey("wandb_entity")
    private val KEY_WANDB_PROJECT     = stringPreferencesKey("wandb_project")
    private val KEY_WANDB_API_KEY     = stringPreferencesKey("wandb_api_key")

    val robotIp: Flow<String> = context.dataStore.data.map { preferences ->
        preferences[KEY_ROBOT_IP] ?: Constants.DEFAULT_ROBOT_IP
    }

    val robotPort: Flow<Int> = context.dataStore.data.map { preferences ->
        preferences[KEY_ROBOT_PORT] ?: Constants.DEFAULT_ROSBRIDGE_PORT
    }

    val prometheusBase: Flow<String> = context.dataStore.data.map { it[KEY_PROMETHEUS_BASE] ?: "http://192.168.1.100:9090" }
    val alertManagerBase: Flow<String> = context.dataStore.data.map { it[KEY_ALERTMANAGER_BASE] ?: "http://192.168.1.100:9093" }
    val lokiBase: Flow<String> = context.dataStore.data.map { it[KEY_LOKI_BASE] ?: "http://192.168.1.100:3100" }
    val wandbEntity: Flow<String> = context.dataStore.data.map { it[KEY_WANDB_ENTITY] ?: "" }
    val wandbProject: Flow<String> = context.dataStore.data.map { it[KEY_WANDB_PROJECT] ?: "" }
    val wandbApiKey: Flow<String> = context.dataStore.data.map { it[KEY_WANDB_API_KEY] ?: "" }

    suspend fun saveConnectionSettings(ip: String, port: Int) {
        context.dataStore.edit { preferences ->
            preferences[KEY_ROBOT_IP] = ip
            preferences[KEY_ROBOT_PORT] = port
        }
    }

    suspend fun saveObservabilitySettings(
        prometheusBase: String,
        alertManagerBase: String,
        lokiBase: String,
        wandbEntity: String,
        wandbProject: String,
        wandbApiKey: String
    ) {
        context.dataStore.edit {
            it[KEY_PROMETHEUS_BASE]   = prometheusBase
            it[KEY_ALERTMANAGER_BASE] = alertManagerBase
            it[KEY_LOKI_BASE]         = lokiBase
            it[KEY_WANDB_ENTITY]      = wandbEntity
            it[KEY_WANDB_PROJECT]     = wandbProject
            it[KEY_WANDB_API_KEY]     = wandbApiKey
        }
    }
}
