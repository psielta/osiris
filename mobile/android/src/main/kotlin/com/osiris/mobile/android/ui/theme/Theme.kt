package com.osiris.mobile.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightColors = lightColorScheme(
    primary = Amber,
    onPrimary = Slate950,
    secondary = Slate950,
    onSecondary = White,
    background = Slate100,
    onBackground = Slate950,
    surface = White,
    onSurface = Slate950,
    error = OsirisRed,
    onError = White,
)

private val DarkColors = darkColorScheme(
    primary = Amber,
    onPrimary = Slate950,
    secondary = Amber,
    onSecondary = Slate950,
    background = Slate950,
    onBackground = Slate100,
    surface = Slate950,
    onSurface = Slate100,
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
