package com.netprove.app.ui.theme

import android.app.Activity
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat

private val DarkColorScheme = darkColorScheme(
    primary = Accent,
    onPrimary = DarkTextPrimary,
    secondary = Cyan,
    onSecondary = DarkTextPrimary,
    tertiary = Success,
    background = DarkBgMain,
    surface = DarkBgCard,
    surfaceVariant = DarkBgSecondary,
    onBackground = DarkTextPrimary,
    onSurface = DarkTextPrimary,
    onSurfaceVariant = DarkTextSub,
    error = Danger,
    onError = DarkTextPrimary,
    outline = DarkTextMuted
)

private val LightColorScheme = lightColorScheme(
    primary = AccentLight,
    onPrimary = LightBgMain,
    secondary = Cyan,
    onSecondary = LightTextPrimary,
    tertiary = Success,
    background = LightBgMain,
    surface = LightBgCard,
    surfaceVariant = LightBgSecondary,
    onBackground = LightTextPrimary,
    onSurface = LightTextPrimary,
    onSurfaceVariant = LightTextSub,
    error = Danger,
    onError = LightBgMain,
    outline = LightTextMuted
)

@Composable
fun NetProveTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = colorScheme.background.toArgb()
            window.navigationBarColor = colorScheme.background.toArgb()
            WindowCompat.getInsetsController(window, view).apply {
                isAppearanceLightStatusBars = !darkTheme
                isAppearanceLightNavigationBars = !darkTheme
            }
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
