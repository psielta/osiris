package com.osiris.mobile.android.ui.components

import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.osiris.mobile.android.R

@Composable
fun OsirisLogo(
    modifier: Modifier = Modifier,
    size: Dp = 72.dp,
    tint: Color = MaterialTheme.colorScheme.onSurface,
) {
    Icon(
        painter = painterResource(R.drawable.ic_osiris_logo),
        contentDescription = "Osiris",
        tint = tint,
        modifier = modifier.size(size),
    )
}
