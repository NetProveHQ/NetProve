package com.netprove.app.ui.navigation

import androidx.compose.animation.*
import androidx.compose.animation.core.tween
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

private const val ANIM_DURATION = 300

@Composable
fun NavGraph(navController: NavHostController, modifier: Modifier = Modifier) {
    NavHost(
        navController = navController,
        startDestination = Screen.Dashboard.route,
        modifier = modifier,
        enterTransition = { fadeIn(tween(ANIM_DURATION)) + slideInHorizontally(tween(ANIM_DURATION)) { it / 4 } },
        exitTransition = { fadeOut(tween(ANIM_DURATION)) },
        popEnterTransition = { fadeIn(tween(ANIM_DURATION)) + slideInHorizontally(tween(ANIM_DURATION)) { -it / 4 } },
        popExitTransition = { fadeOut(tween(ANIM_DURATION)) }
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
