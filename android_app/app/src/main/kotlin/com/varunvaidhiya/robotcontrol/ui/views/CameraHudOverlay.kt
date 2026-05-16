package com.varunvaidhiya.robotcontrol.ui.views

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.DashPathEffect
import android.graphics.Paint
import android.util.AttributeSet
import android.view.View

/** Semi-transparent HUD overlay drawn on top of the MJPEG camera feed. */
class CameraHudOverlay @JvmOverloads constructor(
    context: Context, attrs: AttributeSet? = null
) : View(context, attrs) {

    private val cyan = Color.parseColor("#00E5FF")

    private val gridPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = cyan; alpha = 20; strokeWidth = 1f; style = Paint.Style.STROKE
    }
    private val crossPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = cyan; alpha = 100; strokeWidth = 2f; style = Paint.Style.STROKE
    }
    private val bracketPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = cyan; alpha = 153; strokeWidth = 4f; style = Paint.Style.STROKE
    }
    private val ringPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = cyan; alpha = 30; strokeWidth = 1f; style = Paint.Style.STROKE
        pathEffect = DashPathEffect(floatArrayOf(8f, 8f), 0f)
    }

    override fun onDraw(canvas: Canvas) {
        val w = width.toFloat(); val h = height.toFloat()
        val cx = w / 2f; val cy = h / 2f
        val dp = resources.displayMetrics.density

        // Grid: 3 horizontal + 4 vertical lines
        gridPaint.alpha = 20
        for (i in 1..3) canvas.drawLine(0f, h * i / 4, w, h * i / 4, gridPaint)
        for (i in 1..4) canvas.drawLine(w * i / 5, 0f, w * i / 5, h, gridPaint)

        // Central crosshair circle r=13dp
        val cr = 13f * dp
        crossPaint.alpha = 178
        crossPaint.style = Paint.Style.STROKE
        canvas.drawCircle(cx, cy, cr, crossPaint)

        // Crosshair lines (gap around circle)
        crossPaint.alpha = 100
        canvas.drawLine(cx, 0f, cx, cy - cr - 4f * dp, crossPaint)
        canvas.drawLine(cx, cy + cr + 4f * dp, cx, h, crossPaint)
        canvas.drawLine(0f, cy, cx - cr - 4f * dp, cy, crossPaint)
        canvas.drawLine(cx + cr + 4f * dp, cy, w, cy, crossPaint)

        // Corner brackets (18dp legs)
        val leg = 18f * dp
        val m = 5f * dp
        bracketPaint.alpha = 153
        // Top-left
        canvas.drawLine(m, m, m + leg, m, bracketPaint)
        canvas.drawLine(m, m, m, m + leg, bracketPaint)
        // Top-right
        canvas.drawLine(w - m, m, w - m - leg, m, bracketPaint)
        canvas.drawLine(w - m, m, w - m, m + leg, bracketPaint)
        // Bottom-left
        canvas.drawLine(m, h - m, m + leg, h - m, bracketPaint)
        canvas.drawLine(m, h - m, m, h - m - leg, bracketPaint)
        // Bottom-right
        canvas.drawLine(w - m, h - m, w - m - leg, h - m, bracketPaint)
        canvas.drawLine(w - m, h - m, w - m, h - m - leg, bracketPaint)

        // Scan rings: 36dp and 58dp dashed
        canvas.drawCircle(cx, cy, 36f * dp, ringPaint)
        ringPaint.alpha = 17
        canvas.drawCircle(cx, cy, 58f * dp, ringPaint)
    }
}
