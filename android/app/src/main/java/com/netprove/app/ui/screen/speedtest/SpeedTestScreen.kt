package com.netprove.app.ui.screen.speedtest

import androidx.compose.foundation.layout.*
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
import com.netprove.app.model.SpeedTestPhase
import com.netprove.app.ui.theme.*

@Composable
fun SpeedTestScreen(
    viewModel: SpeedTestViewModel = hiltViewModel()
) {
    val phase by viewModel.phase.collectAsStateWithLifecycle()
    val progress by viewModel.progress.collectAsStateWithLifecycle()
    val currentSpeed by viewModel.currentSpeed.collectAsStateWithLifecycle()
    val result by viewModel.result.collectAsStateWithLifecycle()
    val isRunning by viewModel.isRunning.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(24.dp)
    ) {
        Spacer(modifier = Modifier.height(32.dp))

        // Speed gauge
        Box(contentAlignment = Alignment.Center) {
            CircularProgressIndicator(
                progress = { (progress / 100).toFloat() },
                modifier = Modifier.size(200.dp),
                strokeWidth = 12.dp,
                color = when (phase) {
                    SpeedTestPhase.Download -> Accent
                    SpeedTestPhase.Upload -> Cyan
                    SpeedTestPhase.Complete -> Success
                    else -> MaterialTheme.colorScheme.surfaceVariant
                },
                trackColor = MaterialTheme.colorScheme.surfaceVariant
            )
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = if (isRunning) "%.1f".format(currentSpeed) else "—",
                    style = MaterialTheme.typography.headlineLarge.copy(
                        fontSize = 36.sp,
                        fontWeight = FontWeight.Bold
                    )
                )
                Text(
                    text = stringResource(R.string.mbps),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                if (isRunning) {
                    Text(
                        text = when (phase) {
                            SpeedTestPhase.Ping -> "Ping..."
                            SpeedTestPhase.Download -> stringResource(R.string.download)
                            SpeedTestPhase.Upload -> stringResource(R.string.upload)
                            else -> ""
                        },
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.primary
                    )
                }
            }
        }

        // Results
        result?.let { r ->
            Card(
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    ResultRow(Icons.Default.Speed, "Ping", "%.0f ms".format(r.pingMs), Success)
                    HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                    ResultRow(Icons.Default.ArrowDownward, stringResource(R.string.download), "%.2f Mbps".format(r.downloadMbps), Accent)
                    HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                    ResultRow(Icons.Default.ArrowUpward, stringResource(R.string.upload), "%.2f Mbps".format(r.uploadMbps), Cyan)
                }
            }
        }

        Spacer(modifier = Modifier.weight(1f))

        // Start button
        Button(
            onClick = { viewModel.runTest() },
            enabled = !isRunning,
            modifier = Modifier
                .fillMaxWidth()
                .height(56.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Accent)
        ) {
            Icon(Icons.Default.PlayArrow, contentDescription = null)
            Spacer(modifier = Modifier.width(8.dp))
            Text(
                if (isRunning) stringResource(R.string.testing)
                else stringResource(R.string.run_speed_test),
                fontSize = 16.sp
            )
        }

        Spacer(modifier = Modifier.height(80.dp))
    }
}

@Composable
private fun ResultRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    value: String,
    color: androidx.compose.ui.graphics.Color
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(icon, contentDescription = null, tint = color, modifier = Modifier.size(24.dp))
            Spacer(modifier = Modifier.width(12.dp))
            Text(label, style = MaterialTheme.typography.bodyLarge)
        }
        Text(value, fontWeight = FontWeight.Bold, color = color)
    }
}
