package com.osiris.mobile.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightColors = lightColorScheme(
    primary = Amber,
    onPrimary = Slate950,
    primaryContainer = Amber,
    onPrimaryContainer = Slate950,
    secondary = Slate950,
    onSecondary = White,
    secondaryContainer = Amber100,
    onSecondaryContainer = Slate950,
    tertiary = Amber,
    onTertiary = Slate950,
    tertiaryContainer = Amber100,
    onTertiaryContainer = Slate950,
    background = Slate100,
    onBackground = Slate950,
    surface = White,
    onSurface = Slate950,
    surfaceVariant = Slate200,
    onSurfaceVariant = Slate700,
    outline = Slate700,
    outlineVariant = Slate200,
    error = OsirisRed,
    onError = White,
)

private val DarkColors = darkColorScheme(
    primary = Amber,
    onPrimary = Slate950,
    primaryContainer = Amber,
    onPrimaryContainer = Slate950,
    secondary = Amber,
    onSecondary = Slate950,
    secondaryContainer = Slate800,
    onSecondaryContainer = Slate100,
    tertiary = Amber,
    onTertiary = Slate950,
    tertiaryContainer = Slate800,
    onTertiaryContainer = Slate100,
    background = Slate950,
    onBackground = Slate100,
    surface = Slate950,
    onSurface = Slate100,
    surfaceVariant = Slate800,
    onSurfaceVariant = Slate200,
    outline = Slate200,
    outlineVariant = Slate700,
    error = OsirisRed,
    onError = White,
)

@Composable
fun OsirisTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit,
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkColors else LightColors,
        content = content,
    )
}
