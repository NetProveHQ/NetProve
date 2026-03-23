package com.netprove.app.ui.screen.gaming

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.netprove.app.ui.theme.*

@Composable
fun GamingScreen(
    viewModel: GamingViewModel = hiltViewModel()
) {
    val activeGame by viewModel.activeGame.collectAsStateWithLifecycle()
    val lagWarning by viewModel.lagWarning.collectAsStateWithLifecycle()
    val prediction by viewModel.prediction.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Game status
        Card(
            colors = CardDefaults.cardColors(
                containerColor = if (activeGame != null) StatusGreenBg else MaterialTheme.colorScheme.surface
            )
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(20.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    Icons.Default.SportsEsports,
                    contentDescription = null,
                    tint = if (activeGame != null) Success else MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.size(40.dp)
                )
                Spacer(modifier = Modifier.width(16.dp))
                Column {
                    Text(
                        activeGame ?: "Oyun algılanmadı",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        if (activeGame != null) "Çalışıyor" else "Bekleniyor...",
                        style = MaterialTheme.typography.bodyMedium,
                        color = if (activeGame != null) Success else MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }

        // Lag warning banner
        lagWarning?.let { warning ->
            Card(
                colors = CardDefaults.cardColors(containerColor = StatusRedBg)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Warning,
                        contentDescription = null,
                        tint = Warning,
                        modifier = Modifier.size(24.dp)
                    )
                    Spacer(modifier = Modifier.width(12.dp))
                    Column(modifier = Modifier.weight(1f)) {
                        Text("Gecikme Tahmini", fontWeight = FontWeight.SemiBold, color = Warning)
                        Text(warning, style = MaterialTheme.typography.bodySmall)
                        prediction?.let { p ->
                            Text(
                                "Güven: %.0f%% | ~${p.estimatedSecondsUntilLag}s".format(p.confidence),
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                }
            }
        }

        // Features info
        Text("Oyun Özellikleri", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)

        FeatureCard(Icons.Default.Visibility, "Otomatik Oyun Algılama", "50+ popüler oyunu otomatik algılar", true)
        FeatureCard(Icons.Default.TrendingUp, "Gecikme Tahmini", "CPU, RAM, ping trendlerini analiz ederek lag tahmin eder", true)
        FeatureCard(Icons.Default.DoNotDisturb, "Rahatsız Etmeyin", "Oyun sırasında bildirimleri sessize alır", false)
        FeatureCard(Icons.Default.CleaningServices, "Arka Plan Temizleme", "Gereksiz uygulamaları kapatarak RAM boşaltır", false)

        Spacer(modifier = Modifier.height(80.dp))
    }
}

@Composable
private fun FeatureCard(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    title: String,
    description: String,
    enabled: Boolean
) {
    Card(colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(icon, null, tint = if (enabled) Accent else MaterialTheme.colorScheme.outline, modifier = Modifier.size(28.dp))
            Spacer(modifier = Modifier.width(16.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(title, fontWeight = FontWeight.Medium)
                Text(description, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            if (enabled) {
                Icon(Icons.Default.CheckCircle, null, tint = Success, modifier = Modifier.size(20.dp))
            }
        }
    }
}
