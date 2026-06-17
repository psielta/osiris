package com.osiris.mobile.android.feature.cards

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDateField
import com.osiris.mobile.presentation.cards.DateRangeFilterUiState

@Composable
fun DateRangeFilterControls(
    title: String,
    range: DateRangeFilterUiState,
    filterError: String?,
    onCurrentMonth: () -> Unit,
    onNextMonth: () -> Unit,
    onFromChange: (String) -> Unit,
    onToChange: (String) -> Unit,
    onApply: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Card(modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Text(title, style = MaterialTheme.typography.labelLarge)
                Text(
                    range.label,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                OutlinedButton(onClick = onCurrentMonth, modifier = Modifier.weight(1f)) {
                    Text(stringResource(R.string.filter_current_month))
                }
                OutlinedButton(onClick = onNextMonth, modifier = Modifier.weight(1f)) {
                    Text(stringResource(R.string.filter_next_month))
                }
            }
            OsirisDateField(
                label = stringResource(R.string.filter_from),
                value = range.customFrom,
                onValueChange = onFromChange,
            )
            OsirisDateField(
                label = stringResource(R.string.filter_to),
                value = range.customTo,
                onValueChange = onToChange,
            )
            Button(onClick = onApply, modifier = Modifier.fillMaxWidth()) {
                Text(stringResource(R.string.filter_apply))
            }
            if (!filterError.isNullOrBlank()) {
                Text(
                    text = filterError,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.error,
                )
            }
        }
    }
}
