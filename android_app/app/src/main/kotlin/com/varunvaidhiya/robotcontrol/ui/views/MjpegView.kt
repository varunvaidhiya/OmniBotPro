package com.varunvaidhiya.robotcontrol.ui.views

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Rect
import android.util.AttributeSet
import android.view.View
import kotlinx.coroutines.*
import timber.log.Timber
import java.io.BufferedInputStream
import java.net.HttpURLConnection
import java.net.URL

/**
 * Custom View to display MJPEG stream from ROS web_video_server
 */
class MjpegView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    private var streamUrl: String? = null
    private var isStreaming = false
    private var streamJob: Job? = null
    private var connection: HttpURLConnection? = null
    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())
    
    private var currentBitmap: Bitmap? = null
    private val srcRect = Rect()
    private val dstRect = Rect()
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG)
    
    // UI state
    private var showOverlay = true
    private val overlayPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.WHITE
        textSize = 40f
        textAlign = Paint.Align.CENTER
    }

    fun startStream(url: String) {
        if (isStreaming && url == streamUrl) return
        
        stopStream()
        streamUrl = url
        isStreaming = true
        
        streamJob = scope.launch {
            var conn: HttpURLConnection? = null
            try {
                Timber.d("Starting MJPEG stream: $url")
                conn = URL(url).openConnection() as HttpURLConnection
                connection = conn
                conn.connectTimeout = 5000
                conn.readTimeout = 0  // infinite — MJPEG streams push frames at variable rates
                conn.doInput = true
                conn.connect()

                val inputStream = BufferedInputStream(conn.inputStream)
                
                while (isActive) {
                    val frame = readMjpegFrame(inputStream)
                    if (frame != null) {
                        currentBitmap = frame
                        postInvalidate()
                    } else if (!isActive) {
                        break
                    }
                }
            } catch (e: Exception) {
                if (isActive) {
                    Timber.e(e, "MJPEG Stream error")
                }
            } finally {
                conn?.disconnect()
                connection = null
            }
        }
    }

    fun stopStream() {
        isStreaming = false
        streamJob?.cancel()
        streamJob = null
        connection?.disconnect()
        connection = null
        currentBitmap = null
        invalidate()
    }

    /**
     * Reads one JPEG frame from a multipart/x-mixed-replace MJPEG stream.
     *
     * Protocol (RFC 2046 multipart):
     *   --boundary\r\n
     *   Content-Type: image/jpeg\r\n
     *   Content-Length: NNNN\r\n
     *   \r\n
     *   [NNNN bytes of JPEG data]
     *   \r\n
     *   --boundary\r\n  ...
     *
     * Primary path  – Content-Length header present: read exactly N bytes and
     *                 decode with BitmapFactory.decodeByteArray (fast, reliable).
     * Fallback path – no Content-Length: scan raw bytes for JPEG SOI (0xFF 0xD8)
     *                 and EOI (0xFF 0xD9) markers.
     */
    private fun readMjpegFrame(bis: BufferedInputStream): Bitmap? {
        return try {
            // --- Step 1: advance to the next MIME part boundary ---
            // A boundary line starts with "--". We allow up to 100 lines of
            // padding/preamble before giving up.
            var line = readStreamLine(bis)
            var attempts = 0
            while (!line.startsWith("--") && ++attempts < 100) {
                line = readStreamLine(bis)
            }
            if (!line.startsWith("--")) {
                Timber.w("MJPEG: boundary not found within $attempts lines")
                return null
            }

            // --- Step 2: parse MIME part headers ---
            var contentLength = -1
            while (true) {
                line = readStreamLine(bis)
                if (line.isEmpty()) break  // blank line == end of headers
                if (line.startsWith("Content-Length:", ignoreCase = true)) {
                    contentLength = line.substringAfter(':').trim().toIntOrNull() ?: -1
                }
            }

            // --- Step 3: decode the JPEG frame ---
            if (contentLength > 0) {
                // Fast path: exact byte read
                val bytes = ByteArray(contentLength)
                var offset = 0
                while (offset < contentLength) {
                    val n = bis.read(bytes, offset, contentLength - offset)
                    if (n == -1) return null
                    offset += n
                }
                BitmapFactory.decodeByteArray(bytes, 0, contentLength)
            } else {
                // Fallback: scan for SOI / EOI JPEG markers
                readJpegByMarkers(bis)
            }
        } catch (e: Exception) {
            Timber.w(e, "MJPEG frame read error")
            null
        }
    }

    /**
     * Reads one text line from [bis], consuming bytes until `\n` (stripping any
     * trailing `\r`). Safe to use on a raw stream — does not over-read.
     */
    private fun readStreamLine(bis: BufferedInputStream): String {
        val sb = StringBuilder()
        while (true) {
            val b = bis.read()
            if (b == -1 || b == '\n'.code) break
            if (b != '\r'.code) sb.append(b.toChar())
        }
        return sb.toString()
    }

    /**
     * Fallback JPEG frame reader: accumulates bytes between the JPEG
     * Start-Of-Image marker (0xFF 0xD8) and End-Of-Image marker (0xFF 0xD9).
     */
    private fun readJpegByMarkers(bis: BufferedInputStream): Bitmap? {
        val buffer = ArrayList<Byte>(65536)
        var inJpeg = false
        var prev = -1
        while (true) {
            val b = bis.read()
            if (b == -1) return null
            if (!inJpeg) {
                if (prev == 0xFF && b == 0xD8) {
                    inJpeg = true
                    buffer.add(0xFF.toByte())
                    buffer.add(0xD8.toByte())
                }
            } else {
                buffer.add(b.toByte())
                if (prev == 0xFF && b == 0xD9) {
                    val bytes = buffer.toByteArray()
                    return BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
                }
            }
            prev = b
        }
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        
        canvas.drawColor(Color.BLACK)
        
        currentBitmap?.let { bmp ->
            srcRect.set(0, 0, bmp.width, bmp.height)
            
            // Center crop/fit logic
            val viewWidth = width.toFloat()
            val viewHeight = height.toFloat()
            val scale = minOf(viewWidth / bmp.width, viewHeight / bmp.height)
            
            val scaledWidth = bmp.width * scale
            val scaledHeight = bmp.height * scale
            
            val left = (viewWidth - scaledWidth) / 2
            val top = (viewHeight - scaledHeight) / 2
            
            dstRect.set(left.toInt(), top.toInt(), (left + scaledWidth).toInt(), (top + scaledHeight).toInt())
            
            canvas.drawBitmap(bmp, srcRect, dstRect, paint)
        } ?: run {
            // Draw placeholder text
            canvas.drawText("No Signal", width / 2f, height / 2f, overlayPaint)
        }
    }
    
    override fun onDetachedFromWindow() {
        super.onDetachedFromWindow()
        stopStream()
    }
}
