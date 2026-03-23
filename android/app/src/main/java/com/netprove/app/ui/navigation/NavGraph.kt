package com.netprove.app.ui.navigation

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import com.netprove.app.ui.screen.dashboard.DashboardScreen
import com.netprove.app.ui.screen.devices.DevicesScreen
import com.netprove.app.ui.screen.dns.DnsBenchmarkScreen
import com.netprove.app.ui.screen.gaming.GamingScreen
import com.netprove.app.ui.screen.network.NetworkScreen
import com.netprove.app.ui.screen.reports.ReportsScreen
import com.netprove.app.ui.screen.settings.SettingsScreen
import com.netprove.app.ui.screen.speedtest.SpeedTestScreen

@Composable
fun NavGraph(navController: NavHostController, modifier: Modifier = Modifier) {
    NavHost(
        navController = navController,
        startDestination = Screen.Dashboard.route,
        modifier = modifier
    ) {
        composable(Screen.Dashboard.route) {
            DashboardScreen(
                onNavigateToReports = {
                    navController.navigate(Screen.Reports.route) {
                        launchSingleTop = true
                    }
                }
            )
        }
        composable(Screen.Network.route) {
            NetworkScreen(
                onNavigateToDns = {
                    navController.navigate(Screen.DnsBenchmark.route) {
                        launchSingleTop = true
                    }
                },
                onNavigateToDevices = {
                    navController.navigate(Screen.Devices.route) {
                        launchSingleTop = true
                    }
                }
            )
        }
        composable(Screen.Gaming.route) {
            GamingScreen()
        }
        composable(Screen.Devices.route) {
            DevicesScreen()
        }
        composable(Screen.SpeedTest.route) {
            SpeedTestScreen()
        }
        composable(Screen.DnsBenchmark.route) {
            DnsBenchmarkScreen()
        }
        composable(Screen.Reports.route) {
            ReportsScreen()
        }
        composable(Screen.Settings.route) {
            SettingsScreen()
        }
    }
}
