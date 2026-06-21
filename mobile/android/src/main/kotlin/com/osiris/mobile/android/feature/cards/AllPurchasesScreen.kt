package com.osiris.mobile.android.feature.cards

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.CreditCardPurchaseOverview
import com.osiris.mobile.presentation.cards.AllPurchasesViewModel
import org.koin.androidx.compose.koinViewModel

/**
 * Body of the "Compras" sub-tab inside [CardsHubScreen]: all card purchases across cards, with a date
 * filter. The hub provides the surrounding scaffold/top bar.
 */
@Composable
fun AllPurchasesContent(
    modifier: Modifier,
    onOpenPurchase: (String, String) -> Unit,
    viewModel: AllPurchasesViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    when {
        state.isLoading -> Box(modifier, Alignment.Center) {
            CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
        }

        state.error != null -> Box(modifier.padding(24.dp), Alignment.Center) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(state.error!!, color = MaterialTheme.colorScheme.error)
                Spacer(Modifier.height(12.dp))
                TextButton(onClick = viewModel::load) { Text(stringResource(R.string.retry)) }
            }
        }

        else -> LazyColumn(
            modifier = modifier,
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            item {
                DateRangeFilterControls(
                    title = stringResource(R.string.filter_purchase_date),
                    range = state.range,
                    filterError = state.filterError,
                    onCurrentMonth = viewModel::selectCurrentMonth,
                    onNextMonth = viewModel::selectNextMonth,
                    onFromChange = viewModel::onCustomFromChange,
                    onToChange = viewModel::onCustomToChange,
                    onApply = viewModel::applyCustomRange,
                )
            }
            if (state.purchases.isEmpty()) {
                item {
                    Text(
                        text = stringResource(R.string.purchases_empty_period),
                        modifier = Modifier.fillMaxWidth().padding(24.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        textAlign = TextAlign.Center,
                    )
                }
            } else {
                items(state.purchases, key = { it.id }) { purchase ->
                    PurchaseRow(purchase, onClick = { onOpenPurchase(purchase.creditCardId, purchase.id) })
                }
            }
        }
    }
}

@Composable
private fun PurchaseRow(purchase: CreditCardPurchaseOverview, onClick: () -> Unit) {
    Card(Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Row(Modifier.padding(16.dp), verticalAlignment = Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {
                Text(purchase.description, style = MaterialTheme.typography.bodyLarge)
                Text(
                    text = "${purchase.creditCardName} - ${formatDate(purchase.purchaseDate)} - ${purchase.installments}x",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                if (!purchase.categoryName.isNullOrBlank()) {
                    Text(purchase.categoryName!!, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
            Text(Money.brl(purchase.totalAmount), style = MaterialTheme.typography.bodyLarge)
        }
    }
}
