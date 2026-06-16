package com.osiris.mobile.android.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.osiris.mobile.android.R
import com.osiris.mobile.domain.model.CategoryType

@Composable
fun TypeSelector(
    selected: CategoryType,
    onSelect: (CategoryType) -> Unit,
    modifier: Modifier = Modifier,
) {
    Row(modifier, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        FilterChip(
            selected = selected == CategoryType.Expense,
            onClick = { onSelect(CategoryType.Expense) },
            label = { Text(stringResource(R.string.category_type_expense)) },
        )
        FilterChip(
            selected = selected == CategoryType.Income,
            onClick = { onSelect(CategoryType.Income) },
            label = { Text(stringResource(R.string.category_type_income)) },
        )
    }
}
