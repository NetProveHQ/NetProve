package com.netprove.app.ui.navigation

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.ui.graphics.vector.ImageVector
import com.netprove.app.R

sealed class Screen(
    val route: String,
    val titleResId: Int,
    val icon: ImageVector
) {
    data object Dashboard : Screen("dashboard", R.string.dashboard, Icons.Default.Dashboard)
    data object Network : Screen("network", R.string.network, Icons.Default.Wifi)
    data object Gaming : Screen("gaming", R.string.gaming, Icons.Default.SportsEsports)
    data object SpeedTest : Screen("speedtest", R.string.speed_test, Icons.Default.Speed)
    data object Settings : Screen("settings", R.string.settings, Icons.Default.Settings)

    // Sub-screens (not in bottom nav)
    data object Devices : Screen("devices", R.string.devices, Icons.Default.Radar)
    data object DnsBenchmark : Screen("dns", R.string.dns, Icons.Default.Dns)
    data object Reports : Screen("reports", R.string.reports, Icons.Default.Assessment)

    companion object {
        val bottomNavItems = listOf(Dashboard, Network, Gaming, SpeedTest, Settings)
    }
}
