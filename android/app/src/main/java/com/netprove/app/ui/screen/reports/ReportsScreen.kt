package com.netprove.app.ui.screen.reports

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.netprove.app.R
import com.netprove.app.model.PerformanceReport
import com.netprove.app.ui.component.CircularGauge
import com.netprove.app.ui.theme.*

@Composable
fun ReportsScreen(
    viewModel: ReportsViewModel = hiltViewModel()
) {
    val report by viewModel.report.collectAsStateWithLifecycle()
    val isCollecting by viewModel.isCollecting.collectAsStateWithLifecycle()
    val sampleCount by viewModel.sampleCount.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(
            text = stringResource(R.string.reports),
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold
        )
        Text(
            text = stringResource(R.string.reports_desc),
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        if (isCollecting) {
            // Collecting state
            Card(
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(32.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    CircularProgressIndicator(color = Accent)
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        stringResource(R.string.collecting_data),
                        fontWeight = FontWeight.SemiBold
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        stringResource(R.string.samples_collected, sampleCount),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedButton(onClick = { viewModel.stopCollecting() }) {
                        Text(stringResource(R.string.stop_early))
                    }
                }
            }
        }

        report?.let { r ->
            ReportContent(r)
        }

        if (!isCollecting && report == null) {
            // Empty state
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 48.dp),
                contentAlignment = Alignment.Center
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(
                        Icons.Default.Assessment,
                        contentDescription = null,
                        modifier = Modifier.size(64.dp),
                        tint = MaterialTheme.colorScheme.outline
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        stringResource(R.string.reports_empty_state),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        textAlign = TextAlign.Center
                    )
                }
            }
        }

        // Generate button
        if (!isCollecting) {
            Button(
                onClick = { viewModel.startCollecting() },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(56.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Accent)
            ) {
                Icon(Icons.Default.PlayArrow, contentDescription = null)
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    stringResource(R.string.generate_report),
                    fontSize = 16.sp
                )
            }
        }

        Spacer(modifier = Modifier.height(80.dp))
    }
}

@Composable
private fun ReportContent(report: PerformanceReport) {
    val scoreColor = when {
        report.score >= 90 -> Success
        report.score >= 75 -> Accent
        report.score >= 55 -> Cyan
        report.score >= 35 -> Warning
        else -> Danger
    }

    // Score card
    Card(
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            CircularGauge(
                value = report.score.toFloat(),
                label = report.rating,
                arcColor = scoreColor
            )
            Spacer(modifier = Modifier.height(8.dp))

            // Stars
            Row {
                repeat(5) { i ->
                    Icon(
                        if (i < report.stars) Icons.Default.Star else Icons.Default.StarBorder,
                        contentDescription = null,
                        tint = if (i < report.stars) Warning else MaterialTheme.colorScheme.outline,
                        modifier = Modifier.size(28.dp)
                    )
                }
            }
        }
    }

    // Metrics breakdown
    Text(
        stringResource(R.string.metrics_breakdown),
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.SemiBold
    )

    Card(
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            MetricRow("Ping", "%.0f ms".format(report.avgPingMs), colorForPing(report.avgPingMs))
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MetricRow("Jitter", "%.1f ms".format(report.avgJitterMs), colorForJitter(report.avgJitterMs))
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MetricRow(
                stringResource(R.string.packet_loss),
                "%.1f%%".format(report.avgPacketLoss),
                if (report.avgPacketLoss > 2) Danger else Success
            )
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MetricRow("CPU", "%.0f%%".format(report.avgCpu), colorForPercent(report.avgCpu))
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MetricRow("RAM", "%.0f%%".format(report.avgRam), colorForPercent(report.avgRam))
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MetricRow(
                stringResource(R.string.lag_spikes),
                "${report.lagSpikeCount}",
                if (report.lagSpikeCount > 5) Danger else if (report.lagSpikeCount > 2) Warning else Success
            )
        }
    }

    // Suggestions
    if (report.suggestions.isNotEmpty()) {
        Text(
            stringResource(R.string.suggestions),
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold
        )

        report.suggestions.forEach { suggestion ->
            Card(
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Lightbulb,
                        contentDescription = null,
                        tint = Warning,
                        modifier = Modifier.size(20.dp)
                    )
                    Spacer(modifier = Modifier.width(12.dp))
                    Text(
                        suggestion,
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
            }
        }
    }
}

@Composable
private fun MetricRow(label: String, value: String, color: androidx.compose.ui.graphics.Color) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(label, style = MaterialTheme.typography.bodyMedium)
        Text(value, fontWeight = FontWeight.Bold, color = color)
    }
}

private fun colorForPing(v: Double) = when {
    v >= 150 -> Danger
    v >= 80 -> Warning
    else -> Success
}

private fun colorForJitter(v: Double) = when {
    v >= 30 -> Danger
    v >= 15 -> Warning
    else -> Success
}

private fun colorForPercent(v: Float) = when {
    v >= 90 -> Danger
    v >= 70 -> Warning
    else -> Accent
}
