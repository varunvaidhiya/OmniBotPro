package com.varunvaidhiya.robotcontrol.ui.views

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.DashPathEffect
import android.graphics.Paint
import android.graphics.RadialGradient
import android.graphics.Shader
import android.util.AttributeSet
import android.view.MotionEvent
import android.view.View
import kotlin.math.min
import kotlin.math.pow
import kotlin.math.sqrt

class VirtualJoystickView @JvmOverloads constructor(
    context: Context, attrs: AttributeSet? = null, defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    private var cx = 0f; private var cy = 0f
    private var baseR = 0f; private var thumbR = 0f; private var maxR = 0f

    private var thumbX = 0f; private var thumbY = 0f
    private var isTouched = false
    private val deadzone = 0.1f

    var onJoystickMoved: ((x: Float, y: Float) -> Unit)? = null

    private val cyan = Color.parseColor("#00E5FF")
    private val cyan2 = Color.parseColor("#80F0FF")

    private val outerPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.argb(81, 0, 229, 255); style = Paint.Style.STROKE; strokeWidth = 3f
    }
    private val midPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.argb(30, 0, 229, 255); style = Paint.Style.STROKE; strokeWidth = 1.5f
        pathEffect = DashPathEffect(floatArrayOf(4f, 12f), 0f)
    }
    private val crossPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.argb(30, 0, 229, 255); style = Paint.Style.STROKE; strokeWidth = 1.5f
        pathEffect = DashPathEffect(floatArrayOf(4f, 8f), 0f)
    }
    private val thumbStroke = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.argb(216, 0, 229, 255); style = Paint.Style.STROKE; strokeWidth = 3.5f
    }
    private val fillPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { style = Paint.Style.FILL }
    private val trailPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { style = Paint.Style.FILL }
    private val dotPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.argb(229, 128, 240, 255); style = Paint.Style.FILL
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        super.onSizeChanged(w, h, oldw, oldh)
        cx = w / 2f; cy = h / 2f
        baseR = min(w, h) / 2f - 4f
        thumbR = baseR * 0.30f
        maxR   = baseR * 0.62f
        resetThumb()
    }

    override fun onDraw(canvas: Canvas) {
        // Outer ring
        outerPaint.pathEffect = null
        canvas.drawCircle(cx, cy, baseR, outerPaint)

        // Inner fill
        fillPaint.color = Color.argb(8, 0, 229, 255); fillPaint.shader = null
        canvas.drawCircle(cx, cy, baseR, fillPaint)

        // Mid ring (dashed)
        canvas.drawCircle(cx, cy, baseR * 0.55f, midPaint)

        // Crosshair lines
        val gap = 8f
        canvas.drawLine(cx, cy + gap, cx, cy + baseR * 0.5f, crossPaint)
        canvas.drawLine(cx, cy - gap, cx, cy - baseR * 0.5f, crossPaint)
        canvas.drawLine(cx + gap, cy, cx + baseR * 0.5f, cy, crossPaint)
        canvas.drawLine(cx - gap, cy, cx - baseR * 0.5f, cy, crossPaint)

        val tx = cx + thumbX; val ty = cy + thumbY

        // Velocity trail
        if (isTouched && (kotlin.math.abs(thumbX) > 2f || kotlin.math.abs(thumbY) > 2f)) {
            for (i in 8 downTo 1) {
                val f = i / 8f
                trailPaint.color = Color.argb((f * 0.08f * 255).toInt(), 0, 229, 255)
                canvas.drawCircle(cx + thumbX * f * 0.7f, cy + thumbY * f * 0.7f,
                    thumbR * f * 0.5f, trailPaint)
            }
        }

        // Thumb fill (radial gradient)
        fillPaint.shader = RadialGradient(tx, ty, thumbR,
            intArrayOf(Color.argb(140, 0, 229, 255), Color.argb(51, 0, 229, 255), Color.argb(12, 0, 229, 255)),
            floatArrayOf(0f, 0.5f, 1f), Shader.TileMode.CLAMP)
        canvas.drawCircle(tx, ty, thumbR, fillPaint)

        // Thumb stroke (glow)
        canvas.drawCircle(tx, ty, thumbR, thumbStroke)

        // Inner dot
        canvas.drawCircle(tx, ty, 5f, dotPaint)
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        when (event.action) {
            MotionEvent.ACTION_DOWN, MotionEvent.ACTION_MOVE -> {
                isTouched = true
                val dx = event.x - cx; val dy = event.y - cy
                val dist = sqrt(dx.pow(2) + dy.pow(2))
                if (dist <= maxR) { thumbX = dx; thumbY = dy }
                else { thumbX = dx / dist * maxR; thumbY = dy / dist * maxR }
                notifyListener(); invalidate()
            }
            MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                isTouched = false
                springBack(); invalidate()
            }
        }
        return true
    }

    private fun springBack() {
        post(object : Runnable {
            override fun run() {
                thumbX *= 0.72f; thumbY *= 0.72f
                notifyListener(); invalidate()
                if (kotlin.math.abs(thumbX) > 0.5f || kotlin.math.abs(thumbY) > 0.5f) post(this)
                else { thumbX = 0f; thumbY = 0f; notifyListener(); invalidate() }
            }
        })
    }

    private fun notifyListener() {
        var nx = thumbX / maxR; var ny = -(thumbY / maxR)
        val mag = sqrt(nx.pow(2) + ny.pow(2))
        if (mag < deadzone) { nx = 0f; ny = 0f }
        onJoystickMoved?.invoke(nx, ny)
    }

    private fun resetThumb() { thumbX = 0f; thumbY = 0f }
}
