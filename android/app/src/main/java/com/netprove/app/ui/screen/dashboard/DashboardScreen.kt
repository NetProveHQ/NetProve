package com.netprove.app.ui.screen.dashboard

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
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.netprove.app.R
import com.netprove.app.model.NetworkQuality
import com.netprove.app.ui.component.CircularGauge
import com.netprove.app.ui.component.MetricCard
import com.netprove.app.ui.component.MiniLineChart
import com.netprove.app.ui.theme.*

@Composable
fun DashboardScreen(
    onNavigateToReports: () -> Unit = {},
    viewModel: DashboardViewModel = hiltViewModel()
) {
    val systemMetrics by viewModel.systemMetrics.collectAsStateWithLifecycle()
    val networkMetrics by viewModel.networkMetrics.collectAsStateWithLifecycle()
    val cpuHistory by viewModel.cpuHistory.collectAsStateWithLifecycle()
    val ramHistory by viewModel.ramHistory.collectAsStateWithLifecycle()
    val pingHistory by viewModel.pingHistory.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Network Quality Header
        NetworkQualityCard(networkMetrics.quality)

        // Gauges Row
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            CircularGauge(
                value = systemMetrics.cpuUsagePercent,
                label = "CPU",
                arcColor = colorForPercent(systemMetrics.cpuUsagePercent)
            )
            CircularGauge(
                value = systemMetrics.ramUsagePercent,
                label = "RAM",
                arcColor = colorForPercent(systemMetrics.ramUsagePercent)
            )
            CircularGauge(
                value = networkMetrics.pingMs.toFloat().coerceAtMost(200f),
                maxValue = 200f,
                label = "Ping",
                unit = "ms",
                arcColor = colorForPing(networkMetrics.pingMs)
            )
        }

        // Network Metrics
        Text(
            text = stringResource(R.string.network),
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            MetricCard(
                title = "Ping",
                value = "${networkMetrics.pingMs.toInt()}",
                unit = "ms",
                icon = Icons.Default.Speed,
                valueColor = colorForPing(networkMetrics.pingMs),
                modifier = Modifier.weight(1f)
            )
            MetricCard(
                title = "Jitter",
                value = "%.1f".format(networkMetrics.jitterMs),
                unit = "ms",
                icon = Icons.Default.SwapVert,
                modifier = Modifier.weight(1f)
            )
            MetricCard(
                title = stringResource(R.string.packet_loss),
                value = "%.1f".format(networkMetrics.packetLossPercent),
                unit = "%",
                icon = Icons.Default.Warning,
                valueColor = if (networkMetrics.packetLossPercent > 2) Danger else MaterialTheme.colorScheme.onSurface,
                modifier = Modifier.weight(1f)
            )
        }

        // System Metrics
        Text(
            text = stringResource(R.string.system),
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        // CPU Chart
        Card(
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text("CPU", style = MaterialTheme.typography.labelLarge)
                    Text(
                        "%.1f%%".format(systemMetrics.cpuUsagePercent),
                        color = colorForPercent(systemMetrics.cpuUsagePercent),
                        fontWeight = FontWeight.Bold
                    )
                }
                Spacer(modifier = Modifier.height(8.dp))
                MiniLineChart(
                    data = cpuHistory,
                    lineColor = Accent,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(60.dp)
                )
            }
        }

        // RAM Chart
        Card(
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text("RAM", style = MaterialTheme.typography.labelLarge)
                    Text(
                        "${systemMetrics.ramUsedMb} / ${systemMetrics.ramTotalMb} MB",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                Spacer(modifier = Modifier.height(8.dp))
                MiniLineChart(
                    data = ramHistory,
                    lineColor = Cyan,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(60.dp)
                )
            }
        }

        // Ping Chart
        Card(
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text("Ping", style = MaterialTheme.typography.labelLarge)
                    Text(
                        "${networkMetrics.pingMs.toInt()} ms",
                        color = colorForPing(networkMetrics.pingMs),
                        fontWeight = FontWeight.Bold
                    )
                }
                Spacer(modifier = Modifier.height(8.dp))
                MiniLineChart(
                    data = pingHistory,
                    lineColor = Success,
                    maxValue = 200.0,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(60.dp)
                )
            }
        }

        // Battery info
        Card(
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        if (systemMetrics.isCharging) Icons.Default.BatteryChargingFull
                        else Icons.Default.BatteryFull,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.primary
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        "${systemMetrics.batteryPercent}%",
                        fontWeight = FontWeight.Bold
                    )
                }
                Text(
                    "%.1f°C".format(systemMetrics.batteryTemperature),
                    color = if (systemMetrics.batteryTemperature > 40) Warning else MaterialTheme.colorScheme.onSurfaceVariant,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }

        // Quick actions
        Card(
            onClick = onNavigateToReports,
            colors = CardDefaults.cardColors(containerColor = StatusBlueBg)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    Icons.Default.Assessment,
                    contentDescription = null,
                    tint = Accent,
                    modifier = Modifier.size(28.dp)
                )
                Spacer(modifier = Modifier.width(12.dp))
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        stringResource(R.string.generate_report),
                        fontWeight = FontWeight.SemiBold
                    )
                    Text(
                        stringResource(R.string.reports_desc),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                Icon(
                    Icons.Default.ChevronRight,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        Spacer(modifier = Modifier.height(80.dp)) // Bottom nav padding
    }
}

@Composable
private fun NetworkQualityCard(quality: NetworkQuality) {
    val (text, color) = when (quality) {
        NetworkQuality.Excellent -> stringResource(R.string.excellent) to Success
        NetworkQuality.Good -> stringResource(R.string.good) to Accent
        NetworkQuality.Fair -> stringResource(R.string.fair) to Warning
        NetworkQuality.Poor -> stringResource(R.string.poor) to Danger
        NetworkQuality.Unknown -> stringResource(R.string.unknown) to MaterialTheme.colorScheme.outline
    }

    Card(
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column {
                Text(
                    stringResource(R.string.network_quality),
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text,
                    style = MaterialTheme.typography.headlineMedium,
                    color = color,
                    fontWeight = FontWeight.Bold
                )
            }
            Icon(
                Icons.Default.NetworkCheck,
                contentDescription = null,
                tint = color,
                modifier = Modifier.size(40.dp)
            )
        }
    }
}

private fun colorForPercent(v: Float) = when {
    v >= 90 -> Danger
    v >= 70 -> Warning
    else -> Accent
}

private fun colorForPing(v: Double) = when {
    v >= 150 -> Danger
    v >= 80 -> Warning
    else -> Success
}
