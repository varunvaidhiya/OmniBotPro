package com.varunvaidhiya.robotcontrol.ui.views

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.DashPathEffect
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.RadialGradient
import android.graphics.Shader
import android.util.AttributeSet
import android.view.SurfaceHolder
import android.view.SurfaceView
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.min
import kotlin.math.sin

/** Full JARVIS-style AI face animation rendered on a SurfaceView background thread. */
class JarvisView @JvmOverloads constructor(
    context: Context, attrs: AttributeSet? = null
) : SurfaceView(context, attrs), SurfaceHolder.Callback {

    enum class AiState { IDLE, PROCESSING, SPEAKING }

    @Volatile var aiState: AiState = AiState.IDLE

    private val cyan  = Color.parseColor("#00E5FF")
    private val cyan2 = Color.parseColor("#80F0FF")
    private val bg    = Color.parseColor("#010D18")

    private val strokePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE; strokeCap = Paint.Cap.ROUND
    }
    private val fillPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { style = Paint.Style.FILL }
    private val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        textAlign = Paint.Align.CENTER; color = Color.argb(102, 0, 229, 255)
    }

    private var thread: RenderThread? = null

    init { holder.addCallback(this) }

    override fun surfaceCreated(h: SurfaceHolder) {
        thread = RenderThread(h).also { it.start() }
    }

    override fun surfaceChanged(h: SurfaceHolder, f: Int, w: Int, h2: Int) {}

    override fun surfaceDestroyed(h: SurfaceHolder) {
        thread?.quit(); thread = null
    }

    private inner class RenderThread(private val sh: SurfaceHolder) : Thread("JarvisRender") {
        @Volatile var running = true

        fun quit() { running = false }

        override fun run() {
            var startNs = System.nanoTime()
            while (running) {
                val now = System.nanoTime()
                val t = (now - startNs) / 1_000_000_000.0   // seconds
                val canvas = sh.lockCanvas() ?: continue
                try { draw(canvas, t) } finally { sh.unlockCanvasAndPost(canvas) }
                val elapsed = System.nanoTime() - now
                val sleepMs = (16_666_666L - elapsed) / 1_000_000L
                if (sleepMs > 0) sleep(sleepMs)
            }
        }
    }

    private fun draw(canvas: Canvas, t: Double) {
        val W = canvas.width.toFloat(); val H = canvas.height.toFloat()
        val cx = W / 2f; val cy = H / 2f
        val dp = resources.displayMetrics.density
        val state = aiState

        canvas.drawColor(bg)

        // ── Scan band ──────────────────────────────────────────────
        val scanY = ((t * 26.0 % (H + 40.0)) - 20.0).toFloat()
        fillPaint.shader = LinearGradient(0f, scanY - 18f, 0f, scanY + 18f,
            intArrayOf(Color.TRANSPARENT, Color.argb(8, 0, 229, 255), Color.TRANSPARENT),
            floatArrayOf(0f, 0.5f, 1f), Shader.TileMode.CLAMP)
        canvas.drawRect(0f, scanY - 18f, W, scanY + 18f, fillPaint)
        fillPaint.shader = null

        // ── Outer bezel + tick marks (r=120dp) ─────────────────────
        val r120 = 120f * dp
        strokePaint.color = Color.argb(51, 0, 229, 255); strokePaint.strokeWidth = 1f
        strokePaint.pathEffect = null
        canvas.drawCircle(cx, cy, r120, strokePaint)
        for (i in 0 until 90) {
            val a = (i / 90.0) * Math.PI * 2
            val major = i % 15 == 0; val mid = i % 5 == 0
            val len = (if (major) 9f else if (mid) 5f else 3f) * dp
            val alpha = (if (major) 0.65f else if (mid) 0.35f else 0.15f)
            strokePaint.color = Color.argb((alpha * 255).toInt(), 0, 229, 255)
            strokePaint.strokeWidth = if (major) 1.5f else 0.7f
            canvas.drawLine(
                cx + cos(a).toFloat() * (r120 - len), cy + sin(a).toFloat() * (r120 - len),
                cx + cos(a).toFloat() * r120,          cy + sin(a).toFloat() * r120,
                strokePaint)
        }

        // ── Ring 1 (r=107dp, 12 segments, rotating +0.18 rad/s) ────
        val r107 = 107f * dp
        val rot1 = (t * 0.18).toFloat()
        strokePaint.pathEffect = null
        for (i in 0 until 12) {
            val a = (i / 12.0) * Math.PI * 2 + rot1
            val bright = i % 3 == 0
            strokePaint.color = Color.argb(if (bright) 173 else 76, 0, 229, 255)
            strokePaint.strokeWidth = if (bright) 2f else 1f
            val gap = 0.07f
            drawArcSegment(canvas, cx, cy, r107, a + gap, a + Math.PI / 6.0 - gap)
        }

        // ── Ring 2 (r=94dp, 8 segments, counter-rotating) ──────────
        val r94 = 94f * dp; val rot2 = -(t * 0.26).toFloat()
        for (i in 0 until 8) {
            val a = (i / 8.0) * Math.PI * 2 + rot2
            val even = i % 2 == 0
            strokePaint.color = Color.argb(if (even) 114 else 46, 0, 229, 255)
            strokePaint.strokeWidth = 1.5f
            strokePaint.pathEffect = if (even) null else DashPathEffect(floatArrayOf(4f * dp, 8f * dp), 0f)
            drawArcSegment(canvas, cx, cy, r94, a + 0.09f, a + Math.PI / 4.0 - 0.09f)
        }
        strokePaint.pathEffect = null

        // ── Ring 3 (r=82dp, full dashed, rotating +0.07) ───────────
        val r82 = 82f * dp
        strokePaint.color = Color.argb(30, 0, 229, 255); strokePaint.strokeWidth = 0.8f
        strokePaint.pathEffect = DashPathEffect(floatArrayOf(4f * dp, 14f * dp), (t * 0.07 * r82).toFloat())
        canvas.drawCircle(cx, cy, r82, strokePaint)
        strokePaint.pathEffect = null

        // ── Frequency analyser (96 radial bars, base r=72dp) ───────
        val r72 = 72f * dp
        strokePaint.strokeWidth = 1.8f
        for (i in 0 until 96) {
            val ang = (i / 96.0) * Math.PI * 2 - Math.PI / 2
            val h = when (state) {
                AiState.SPEAKING   -> 4.0 + abs(sin(i * 0.38 + t * 9)) * 14 + abs(sin(i * 0.72 + t * 14)) * 7
                AiState.PROCESSING -> 2.0 + abs(sin(i * 0.3  + t * 22)) * 6  + abs(sin(i * 0.6  + t * 18)) * 3
                AiState.IDLE       -> 1.5 + abs(sin(i * 0.18 + t * 1.8)) * 2.5
            }.toFloat() * dp
            val alpha = min(1f, 0.3f + h / (22f * dp))
            strokePaint.color = if (h > 10f * dp)
                Color.argb((alpha * 255).toInt(), 128, 240, 255)
            else
                Color.argb((alpha * 255).toInt(), 0, 229, 255)
            canvas.drawLine(
                cx + cos(ang).toFloat() * r72, cy + sin(ang).toFloat() * r72,
                cx + cos(ang).toFloat() * (r72 + h), cy + sin(ang).toFloat() * (r72 + h),
                strokePaint)
        }

        // ── Face circle (r=68dp) ────────────────────────────────────
        val r68 = 68f * dp
        val pulse = (0.52f + sin(t * 2.2).toFloat() * 0.1f)
        strokePaint.color = Color.argb((pulse * 255).toInt(), 0, 229, 255)
        strokePaint.strokeWidth = 1.8f; strokePaint.pathEffect = null
        canvas.drawCircle(cx, cy, r68, strokePaint)

        // ── Radar sweep (inside face, r=66dp) ──────────────────────
        val r66 = 66f * dp; val radarAngle = (t * 1.4).toFloat()
        for (i in 0 until 38) {
            val a = radarAngle - (i / 38.0) * Math.PI * 0.33
            val alpha = ((1.0 - i / 38.0) * 0.1 * 255).toInt()
            strokePaint.color = Color.argb(alpha, 0, 229, 255); strokePaint.strokeWidth = 1f
            canvas.drawLine(cx, cy, cx + cos(a).toFloat() * r66, cy + sin(a).toFloat() * r66, strokePaint)
        }
        strokePaint.color = Color.argb(148, 0, 229, 255); strokePaint.strokeWidth = 1.5f
        canvas.drawLine(cx, cy, cx + cos(radarAngle) * r66, cy + sin(radarAngle) * r66, strokePaint)

        // ── Arc-reactor core (concentric circles + hex + glow dot) ─
        val corePulse = if (state == AiState.SPEAKING) (0.85f + sin(t * 10).toFloat() * 0.15f)
                        else                           (0.85f + sin(t * 2).toFloat()  * 0.05f)
        for ((idx, r) in listOf(20f, 15f, 10f, 6f).withIndex()) {
            val rDp = r * dp
            val alpha = ((0.18f + idx * 0.12f) * corePulse * 255).toInt()
            strokePaint.color = Color.argb(alpha, 0, 229, 255)
            strokePaint.strokeWidth = if (idx == 3) 2f else 0.8f
            canvas.drawCircle(cx, cy, rDp, strokePaint)
        }
        // Rotating hexagon
        val hexAngle = (t * 0.4).toFloat()
        strokePaint.color = Color.argb((0.8f * corePulse * 255).toInt(), 128, 240, 255)
        strokePaint.strokeWidth = 1.5f
        val hPath = android.graphics.Path()
        for (i in 0..6) {
            val a = (i / 6.0) * Math.PI * 2 + hexAngle
            val hx = cx + cos(a).toFloat() * 8f * dp; val hy = cy + sin(a).toFloat() * 8f * dp
            if (i == 0) hPath.moveTo(hx, hy) else hPath.lineTo(hx, hy)
        }
        hPath.close(); canvas.drawPath(hPath, strokePaint)
        // Center glow dot
        fillPaint.shader = RadialGradient(cx, cy, 5f * dp,
            intArrayOf(Color.argb((corePulse * 255).toInt(), 200, 245, 255), Color.TRANSPARENT),
            null, Shader.TileMode.CLAMP)
        canvas.drawCircle(cx, cy, 5f * dp, fillPaint); fillPaint.shader = null

        // ── Eyes (y=−17dp from centre) ──────────────────────────────
        val eyeY = cy - 17f * dp
        val blinkRaw = sin(t * 0.45); val blink = if (blinkRaw > 0.97) maxOf(0.0, 1.0 - (blinkRaw - 0.97) * 100) else 1.0
        val eyeBright = if (state == AiState.SPEAKING) 0.9f + sin(t * 10).toFloat() * 0.1f
                        else 0.75f + sin(t * 1.5).toFloat() * 0.08f
        for (ox in intArrayOf(-24, 24)) {
            val ex = cx + ox * dp; val ew = 20f * dp; val eh = (5f * blink * eyeBright).toFloat() * dp
            if (eh < 0.5f) continue
            val path = android.graphics.Path().apply {
                moveTo(ex - ew / 2, eyeY); lineTo(ex - ew * 0.2f, eyeY - eh)
                lineTo(ex + ew * 0.2f, eyeY - eh); lineTo(ex + ew / 2, eyeY)
                lineTo(ex + ew * 0.2f, eyeY + eh); lineTo(ex - ew * 0.2f, eyeY + eh); close()
            }
            fillPaint.shader = LinearGradient(ex - ew / 2, eyeY, ex + ew / 2, eyeY,
                intArrayOf(Color.argb((0.15f * eyeBright * 255).toInt(), 0, 229, 255),
                    Color.argb((0.92f * eyeBright * 255).toInt(), 128, 240, 255),
                    Color.argb((0.15f * eyeBright * 255).toInt(), 0, 229, 255)),
                floatArrayOf(0f, 0.5f, 1f), Shader.TileMode.CLAMP)
            canvas.drawPath(path, fillPaint); fillPaint.shader = null
        }

        // ── Waveform mouth (y=+24dp) ────────────────────────────────
        val mY = cy + 24f * dp; val mW = 56f * dp; val segs = 80
        val (a1, f1, a2, f2, a3, f3, sp) = when (state) {
            AiState.SPEAKING   -> listOf(10.0 + sin(t * 4.5) * 5, 2.5, 5.0 + sin(t * 6) * 3, 5.5, 2.5, 9.0, 5.0)
            AiState.PROCESSING -> listOf(1.5, 16.0, 1.0, 22.0, 0.5, 28.0, 12.0)
            AiState.IDLE       -> listOf(2.0 + sin(t * 0.7), 2.5, 0.8, 5.0, 0.3, 8.0, 1.4)
        }
        strokePaint.color = cyan2; strokePaint.strokeWidth = 2.2f; strokePaint.pathEffect = null
        val mPath = android.graphics.Path()
        for (i in 0..segs) {
            val fr = i / segs.toDouble(); val x = (cx - mW + fr * mW * 2).toFloat()
            val ph = fr * Math.PI * 2
            val y = (mY + a1 * sin(f1 * ph + t * sp) + a2 * sin(f2 * ph + t * sp * 1.4) + a3 * sin(f3 * ph + t * sp * 1.9)).toFloat() * dp
            if (i == 0) mPath.moveTo(x, y) else mPath.lineTo(x, y)
        }
        canvas.drawPath(mPath, strokePaint)

        // ── Processing spinner ──────────────────────────────────────
        if (state == AiState.PROCESSING) {
            val r54 = 54f * dp; val r44 = 44f * dp
            strokePaint.color = cyan2; strokePaint.strokeWidth = 2.2f
            drawArcSegment(canvas, cx, cy, r54, t * 3.5, t * 3.5 + Math.PI * 1.25)
            strokePaint.color = Color.argb(122, 0, 229, 255); strokePaint.strokeWidth = 1.5f
            drawArcSegment(canvas, cx, cy, r44, -t * 2.2, -t * 2.2 + Math.PI * 0.7)
        }

        // ── Corner HUD brackets ─────────────────────────────────────
        val leg = 18f * dp; val bm = 6f * dp
        strokePaint.color = Color.argb(122, 0, 229, 255); strokePaint.strokeWidth = 1.8f
        strokePaint.pathEffect = null
        // TL
        canvas.drawLine(bm + leg, bm, bm, bm, strokePaint); canvas.drawLine(bm, bm, bm, bm + leg, strokePaint)
        // TR
        canvas.drawLine(W - bm - leg, bm, W - bm, bm, strokePaint); canvas.drawLine(W - bm, bm, W - bm, bm + leg, strokePaint)
        // BL
        canvas.drawLine(bm + leg, H - bm, bm, H - bm, strokePaint); canvas.drawLine(bm, H - bm, bm, H - bm - leg, strokePaint)
        // BR
        canvas.drawLine(W - bm - leg, H - bm, W - bm, H - bm, strokePaint); canvas.drawLine(W - bm, H - bm, W - bm, H - bm - leg, strokePaint)

        // ── Compass labels ──────────────────────────────────────────
        textPaint.textSize = 7f * dp
        for ((a, label) in listOf(Pair(-Math.PI / 2, "VLA·9D"), Pair(0.17 * Math.PI, "SLAM"), Pair(0.83 * Math.PI, "6-CAM"))) {
            val r112 = 112f * dp
            val lx = cx + cos(a).toFloat() * r112; val ly = cy + sin(a).toFloat() * r112
            textPaint.color = Color.argb((((0.4 + sin(t * 0.9 + a) * 0.08) * 255).toInt()), 0, 229, 255)
            canvas.drawText(label, lx, ly + textPaint.textSize / 3, textPaint)
        }
    }

    private fun drawArcSegment(canvas: Canvas, cx: Float, cy: Float, r: Float, startRad: Double, endRad: Double) {
        val left = cx - r; val top = cy - r; val right = cx + r; val bottom = cy + r
        val startDeg = Math.toDegrees(startRad).toFloat()
        val sweepDeg = Math.toDegrees(endRad - startRad).toFloat()
        canvas.drawArc(left, top, right, bottom, startDeg, sweepDeg, false, strokePaint)
    }

    operator fun <T> List<T>.component6() = this[5]
    operator fun <T> List<T>.component7() = this[6]
}
