package com.netprove.app.ui.screen.network

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
import com.netprove.app.ui.component.MetricCard
import com.netprove.app.ui.component.MiniLineChart
import com.netprove.app.ui.screen.dashboard.DashboardViewModel
import com.netprove.app.ui.theme.*

@Composable
fun NetworkScreen(
    onNavigateToDns: () -> Unit = {},
    onNavigateToDevices: () -> Unit = {},
    viewModel: DashboardViewModel = hiltViewModel()
) {
    val network by viewModel.networkMetrics.collectAsStateWithLifecycle()
    val pingHistory by viewModel.pingHistory.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Quality badge
        Card(
            colors = CardDefaults.cardColors(
                containerColor = when (network.quality) {
                    com.netprove.app.model.NetworkQuality.Excellent -> StatusGreenBg
                    com.netprove.app.model.NetworkQuality.Good -> StatusBlueBg
                    com.netprove.app.model.NetworkQuality.Fair -> StatusBlueBg
                    else -> StatusRedBg
                }
            )
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(20.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.Center
            ) {
                Icon(
                    Icons.Default.NetworkCheck,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.onSurface,
                    modifier = Modifier.size(28.dp)
                )
                Spacer(modifier = Modifier.width(12.dp))
                Text(
                    network.quality.name,
                    style = MaterialTheme.typography.headlineMedium,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        // Metrics grid
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            MetricCard(
                title = "Ping",
                value = "${network.pingMs.toInt()}",
                unit = "ms",
                icon = Icons.Default.Speed,
                valueColor = when {
                    network.pingMs >= 150 -> Danger
                    network.pingMs >= 80 -> Warning
                    else -> Success
                },
                modifier = Modifier.weight(1f)
            )
            MetricCard(
                title = "Jitter",
                value = "%.1f".format(network.jitterMs),
                unit = "ms",
                icon = Icons.Default.SwapVert,
                valueColor = if (network.jitterMs > 20) Warning else MaterialTheme.colorScheme.onSurface,
                modifier = Modifier.weight(1f)
            )
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            MetricCard(
                title = stringResource(R.string.packet_loss),
                value = "%.1f".format(network.packetLossPercent),
                unit = "%",
                icon = Icons.Default.Warning,
                valueColor = if (network.packetLossPercent > 2) Danger else MaterialTheme.colorScheme.onSurface,
                modifier = Modifier.weight(1f)
            )
            MetricCard(
                title = stringResource(R.string.download),
                value = "%.1f".format(network.downloadMbps),
                unit = "Mbps",
                icon = Icons.Default.ArrowDownward,
                modifier = Modifier.weight(1f)
            )
        }

        // Ping history chart
        Card(
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    stringResource(R.string.ping_history),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )
                Spacer(modifier = Modifier.height(12.dp))
                MiniLineChart(
                    data = pingHistory,
                    lineColor = Success,
                    maxValue = 200.0,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(100.dp)
                )
            }
        }

        // Navigation cards
        Text(
            stringResource(R.string.tools),
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold
        )

        Card(
            onClick = onNavigateToDns,
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(Icons.Default.Dns, null, tint = Accent, modifier = Modifier.size(28.dp))
                Spacer(modifier = Modifier.width(12.dp))
                Column(modifier = Modifier.weight(1f)) {
                    Text(stringResource(R.string.dns), fontWeight = FontWeight.SemiBold)
                    Text(
                        stringResource(R.string.dns_benchmark_desc),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                Icon(Icons.Default.ChevronRight, null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }

        Card(
            onClick = onNavigateToDevices,
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(Icons.Default.Radar, null, tint = Cyan, modifier = Modifier.size(28.dp))
                Spacer(modifier = Modifier.width(12.dp))
                Column(modifier = Modifier.weight(1f)) {
                    Text(stringResource(R.string.devices), fontWeight = FontWeight.SemiBold)
                    Text(
                        stringResource(R.string.devices_desc),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                Icon(Icons.Default.ChevronRight, null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }

        Spacer(modifier = Modifier.height(80.dp))
    }
}
