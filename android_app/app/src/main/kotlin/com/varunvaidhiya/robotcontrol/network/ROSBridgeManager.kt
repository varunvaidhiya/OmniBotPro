package com.varunvaidhiya.robotcontrol.network

import com.google.gson.Gson
import kotlinx.coroutines.*
import okhttp3.*
import timber.log.Timber
import java.util.concurrent.TimeUnit

/**
 * Manages WebSocket connection to ROSBridge server
 * Handles automatic reconnection with exponential backoff
 */
class ROSBridgeManager(
    private val serverUrl: String,
    private val listener: ROSBridgeListener
) {
    private val client = OkHttpClient.Builder()
        .readTimeout(0, TimeUnit.MILLISECONDS)
        .build()
    
    private var webSocket: WebSocket? = null
    private val gson = Gson()
    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    
    private var reconnectAttempts = 0
    private val maxReconnectAttempts = 5
    private val baseReconnectDelay = 1000L // 1 second
    
    enum class ConnectionState {
        DISCONNECTED, CONNECTING, CONNECTED, ERROR
    }
    
    @Volatile
    var connectionState = ConnectionState.DISCONNECTED
        private set

    private val serviceCallbacks = mutableMapOf<String, (Map<String, Any>) -> Unit>()
    private var serviceCallId = 0
    
    /**
     * Connect to ROSBridge server
     */
    fun connect() {
        if (connectionState == ConnectionState.CONNECTED || 
            connectionState == ConnectionState.CONNECTING) {
            Timber.w("Already connected or connecting")
            return
        }
        
        connectionState = ConnectionState.CONNECTING
        
        val request = Request.Builder()
            .url(serverUrl)
            .build()
        
        webSocket = client.newWebSocket(request, createWebSocketListener())
    }
    
    /**
     * Disconnect from ROSBridge server
     */
    fun disconnect() {
        webSocket?.close(1000, "Client disconnecting")
        webSocket = null
        connectionState = ConnectionState.DISCONNECTED
        reconnectAttempts = maxReconnectAttempts  // prevent auto-reconnect
        scope.coroutineContext.cancelChildren()
    }
    
    /**
     * Subscribe to a ROS topic
     */
    fun subscribe(topic: String, messageType: String) {
        val message = mapOf(
            "op" to "subscribe",
            "topic" to topic,
            "type" to messageType
        )
        sendMessage(message)
    }
    
    /**
     * Publish message to ROS topic
     */
    fun publish(topic: String, message: Map<String, Any>) {
        val rosMessage = mapOf(
            "op" to "publish",
            "topic" to topic,
            "msg" to message
        )
        sendMessage(rosMessage)
    }
    
    /**
     * Call a ROS service via ROSBridge (std_srvs/Trigger or custom).
     * Callback is invoked on the IO thread when the response arrives.
     */
    fun callService(
        service: String,
        args: Map<String, Any> = emptyMap(),
        id: String = "svc_${++serviceCallId}",
        callback: (Map<String, Any>) -> Unit
    ) {
        serviceCallbacks[id] = callback
        sendMessage(
            mapOf("op" to "call_service", "service" to service, "args" to args, "id" to id)
        )
    }

    /**
     * Send raw message to ROSBridge
     */
    private fun sendMessage(message: Map<String, Any>) {
        if (connectionState != ConnectionState.CONNECTED) {
            Timber.w("Cannot send message - not connected")
            return
        }
        
        val json = gson.toJson(message)
        webSocket?.send(json)
    }
    
    /**
     * Handle automatic reconnection with exponential backoff
     */
    private fun attemptReconnect() {
        if (reconnectAttempts >= maxReconnectAttempts) {
            Timber.e("Max reconnect attempts reached (or manual disconnect)")
            connectionState = ConnectionState.ERROR
            return
        }
        
        val delay = baseReconnectDelay * (1 shl reconnectAttempts) // Exponential: 1s, 2s, 4s, 8s, 16s
        reconnectAttempts++
        
        Timber.i("Reconnecting in ${delay}ms (attempt $reconnectAttempts)")
        
        scope.launch {
            delay(delay)
            connect()
        }
    }
    
    /**
     * Create WebSocket listener
     */
    private fun createWebSocketListener() = object : WebSocketListener() {
        override fun onOpen(webSocket: WebSocket, response: Response) {
            Timber.i("WebSocket connected")
            connectionState = ConnectionState.CONNECTED
            reconnectAttempts = 0
            listener.onConnected()
            
            // Start heartbeat
            startHeartbeat()
        }
        
        override fun onMessage(webSocket: WebSocket, text: String) {
            try {
                @Suppress("UNCHECKED_CAST")
                val message = gson.fromJson(text, Map::class.java) as Map<String, Any>

                when (message["op"] as? String) {
                    "service_response" -> {
                        val id = message["id"] as? String ?: return
                        @Suppress("UNCHECKED_CAST")
                        val values = (message["values"] as? Map<String, Any>) ?: emptyMap<String, Any>()
                        serviceCallbacks.remove(id)?.invoke(values)
                    }
                    "publish" -> {
                        val topic = message["topic"] as? String ?: return
                        @Suppress("UNCHECKED_CAST")
                        val msg = message["msg"] as? Map<String, Any> ?: return
                        listener.onMessageReceived(topic, msg)
                    }
                    else -> {
                        // Fallback: some ROSBridge versions omit the "op" key for topic messages
                        val topic = message["topic"] as? String ?: return
                        @Suppress("UNCHECKED_CAST")
                        val msg = message["msg"] as? Map<String, Any> ?: return
                        listener.onMessageReceived(topic, msg)
                    }
                }
            } catch (e: Exception) {
                Timber.e(e, "Failed to parse message: $text")
            }
        }
        
        override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
            Timber.e(t, "WebSocket error")
            connectionState = ConnectionState.ERROR
            listener.onError(t.message ?: "Unknown error")
            attemptReconnect()
        }
        
        override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
            Timber.i("WebSocket closed: $reason")
            connectionState = ConnectionState.DISCONNECTED
            listener.onDisconnected()
        }
    }
    
    /**
     * Send periodic ping to keep connection alive.
     * OkHttp3 handles TCP keepalive, so this is a lightweight ROS-level
     * liveness signal — sends a valid JSON ping (not empty string).
     */
    private fun startHeartbeat() {
        if (scope.isActive) {
            scope.launch {
                while (isActive && connectionState == ConnectionState.CONNECTED) {
                    delay(5000)
                    if (connectionState != ConnectionState.CONNECTED) break
                    webSocket?.send("""{"op":"ping"}""")
                }
            }
        }
    }
}
