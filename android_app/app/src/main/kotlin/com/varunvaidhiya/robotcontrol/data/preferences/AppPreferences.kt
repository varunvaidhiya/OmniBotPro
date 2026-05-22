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

    private val KEY_ROBOT_IP      = stringPreferencesKey("robot_ip")
    private val KEY_ROBOT_PORT    = intPreferencesKey("robot_port")
    private val KEY_GPU_IP        = stringPreferencesKey("gpu_desktop_ip")
    private val KEY_WANDB_API_KEY = stringPreferencesKey("wandb_api_key")
    private val KEY_WANDB_ENTITY  = stringPreferencesKey("wandb_entity")

    val robotIp: Flow<String> = context.dataStore.data.map { it[KEY_ROBOT_IP] ?: Constants.DEFAULT_ROBOT_IP }
    val robotPort: Flow<Int>  = context.dataStore.data.map { it[KEY_ROBOT_PORT] ?: Constants.DEFAULT_ROSBRIDGE_PORT }
    val gpuDesktopIp: Flow<String> = context.dataStore.data.map { it[KEY_GPU_IP] ?: "192.168.1.100" }
    val wandbApiKey: Flow<String>  = context.dataStore.data.map { it[KEY_WANDB_API_KEY] ?: "" }
    val wandbEntity: Flow<String>  = context.dataStore.data.map { it[KEY_WANDB_ENTITY] ?: "" }

    suspend fun saveConnectionSettings(ip: String, port: Int) {
        context.dataStore.edit { preferences ->
            preferences[KEY_ROBOT_IP]   = ip
            preferences[KEY_ROBOT_PORT] = port
        }
    }

    suspend fun saveObservabilitySettings(gpuIp: String, wandbKey: String, wandbEntity: String) {
        context.dataStore.edit { preferences ->
            preferences[KEY_GPU_IP]        = gpuIp
            preferences[KEY_WANDB_API_KEY] = wandbKey
            preferences[KEY_WANDB_ENTITY]  = wandbEntity
        }
    }
}
