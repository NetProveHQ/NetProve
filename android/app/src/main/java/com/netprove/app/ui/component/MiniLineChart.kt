package com.netprove.app.ui.component

import androidx.compose.foundation.Canvas
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke

@Composable
fun MiniLineChart(
    data: List<Double>,
    lineColor: Color,
    maxValue: Double = 100.0,
    modifier: Modifier = Modifier
) {
    if (data.size < 2) return

    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height
        val maxV = if (maxValue > 0) maxValue else 1.0
        val step = w / (data.size - 1)

        // Build points
        val points = data.mapIndexed { i, value ->
            val x = i * step
            val y = h - ((value / maxV).coerceIn(0.0, 1.0) * h).toFloat()
            Offset(x, y)
        }

        // Smooth curve using Catmull-Rom to cubic Bezier
        val linePath = Path()
        val fillPath = Path()

        linePath.moveTo(points[0].x, points[0].y)
        fillPath.moveTo(points[0].x, h)
        fillPath.lineTo(points[0].x, points[0].y)

        val tension = 0.35f

        for (i in 0 until points.size - 1) {
            val p0 = points[maxOf(i - 1, 0)]
            val p1 = points[i]
            val p2 = points[minOf(i + 1, points.size - 1)]
            val p3 = points[minOf(i + 2, points.size - 1)]

            val cp1x = p1.x + (p2.x - p0.x) * tension
            val cp1y = p1.y + (p2.y - p0.y) * tension
            val cp2x = p2.x - (p3.x - p1.x) * tension
            val cp2y = p2.y - (p3.y - p1.y) * tension

            linePath.cubicTo(cp1x, cp1y, cp2x, cp2y, p2.x, p2.y)
            fillPath.cubicTo(cp1x, cp1y, cp2x, cp2y, p2.x, p2.y)
        }

        // Close fill path
        fillPath.lineTo(points.last().x, h)
        fillPath.close()

        // Draw gradient fill
        drawPath(
            path = fillPath,
            brush = Brush.verticalGradient(
                colors = listOf(lineColor.copy(alpha = 0.3f), lineColor.copy(alpha = 0.0f)),
                startY = 0f,
                endY = h
            )
        )

        // Draw smooth line
        drawPath(
            path = linePath,
            color = lineColor,
            style = Stroke(width = 2.5f)
        )
    }
}
