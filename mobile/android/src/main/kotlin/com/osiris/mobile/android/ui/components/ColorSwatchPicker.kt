package com.osiris.mobile.android.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.osiris.mobile.presentation.categories.CategoryColors

/** Parses a "#RRGGBB" string into an opaque [Color], or null if malformed. */
internal fun parseHexColor(hex: String): Color? =
    runCatching { Color(0xFF000000L or hex.removePrefix("#").toLong(16)) }.getOrNull()

@Composable
fun ColorSwatchPicker(
    selected: String?,
    onSelect: (String?) -> Unit,
    modifier: Modifier = Modifier,
) {
    Row(
        modifier = modifier.horizontalScroll(rememberScrollState()),
        horizontalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        SwatchCircle(color = null, selected = selected == null, onClick = { onSelect(null) })
        CategoryColors.Palette.forEach { hex ->
            SwatchCircle(
                color = hex,
                selected = selected?.equals(hex, ignoreCase = true) == true,
                onClick = { onSelect(hex) },
            )
        }
    }
}

@Composable
private fun SwatchCircle(color: String?, selected: Boolean, onClick: () -> Unit) {
    val fill = remember(color) { color?.let { parseHexColor(it) } }
    val borderColor = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.outlineVariant
    val borderWidth = if (selected) 3.dp else 1.dp
    Box(
        modifier = Modifier
            .size(36.dp)
            .clip(CircleShape)
            .background(fill ?: MaterialTheme.colorScheme.surface)
            .border(borderWidth, borderColor, CircleShape)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center,
    ) {
        if (color == null) {
            Text("∅", color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}
