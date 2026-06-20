package com.osiris.mobile.android.feature.cards

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.CreditCardPurchase
import com.osiris.mobile.domain.model.CreditCardStatement
import com.osiris.mobile.presentation.cards.CardDetailsViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CardDetailsScreen(
    cardId: String,
    onNavigateBack: () -> Unit,
    onEdit: () -> Unit,
    onAddPurchase: () -> Unit,
    onOpenPurchase: (String) -> Unit,
    onOpenStatement: (String) -> Unit,
    viewModel: CardDetailsViewModel = koinViewModel { parametersOf(cardId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var selectedTab by remember { mutableIntStateOf(0) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.card?.name ?: stringResource(R.string.cards_title)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
                actions = {
                    if (state.card?.isActive == true) {
                        IconButton(onClick = onEdit) {
                            Icon(Icons.Filled.Edit, contentDescription = stringResource(R.string.card_edit_action))
                        }
                    }
                },
            )
        },
        floatingActionButton = {
            if (state.card?.isActive == true) {
                ExtendedFloatingActionButton(
                    onClick = onAddPurchase,
                    icon = { Icon(Icons.Filled.Add, contentDescription = null) },
                    text = { Text(stringResource(R.string.purchase_new)) },
                )
            }
        },
    ) { padding ->
        when {
            state.isLoading -> Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }

            state.error != null -> Box(Modifier.fillMaxSize().padding(padding).padding(24.dp), Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(state.error!!, color = MaterialTheme.colorScheme.error)
                    Spacer(Modifier.height(12.dp))
                    TextButton(onClick = viewModel::load) { Text(stringResource(R.string.retry)) }
                }
            }

            state.card != null -> Column(Modifier.fillMaxSize().padding(padding)) {
                LazyColumn(
                    modifier = Modifier.weight(1f),
                    contentPadding = PaddingValues(16.dp),
                ) {
                    item {
                        Summary(state)
                        Spacer(Modifier.height(16.dp))
                        TabRow(selectedTabIndex = selectedTab) {
                            Tab(
                                selected = selectedTab == 0,
                                onClick = { selectedTab = 0 },
                                text = { Text(stringResource(R.string.card_tab_purchases)) },
                            )
                            Tab(
                                selected = selectedTab == 1,
                                onClick = { selectedTab = 1 },
                                text = { Text(stringResource(R.string.card_tab_statements)) },
                            )
                        }
                        Spacer(Modifier.height(12.dp))
                    }
                    if (selectedTab == 0) {
                        if (state.purchases.isEmpty()) {
                            item { EmptyText(stringResource(R.string.purchases_empty)) }
                        } else {
                            items(state.purchases, key = { it.id }) { purchase ->
                                PurchaseRow(purchase, onClick = { onOpenPurchase(purchase.id) })
                                HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                            }
                        }
                    } else {
                        if (state.statements.isEmpty()) {
                            item { EmptyText(stringResource(R.string.statements_empty)) }
                        } else {
                            items(state.statements, key = { it.id }) { statement ->
                                StatementRow(statement, onClick = { onOpenStatement(statement.id) })
                                HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun Summary(state: com.osiris.mobile.presentation.cards.CardDetailsUiState) {
    val card = state.card ?: return
    Column {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            SummaryCard(stringResource(R.string.card_used_limit), state.overview?.usedLimit ?: 0.0, Modifier.weight(1f))
            SummaryCard(stringResource(R.string.card_available_limit), state.overview?.availableLimit ?: card.limit, Modifier.weight(1f))
        }
        Spacer(Modifier.height(12.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            SummaryCard(stringResource(R.string.card_limit_label), card.limit, Modifier.weight(1f))
            SummaryCard(stringResource(R.string.card_future_installments), state.overview?.futureInstallmentsTotal ?: 0.0, Modifier.weight(1f))
        }
        Spacer(Modifier.height(12.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            SummaryCard(
                stringResource(R.string.card_current_statement),
                state.currentStatement?.openBalance ?: 0.0,
                Modifier.weight(1f),
            )
            SummaryCard(
                stringResource(R.string.card_next_statement),
                state.overview?.nextStatement?.openBalance ?: 0.0,
                Modifier.weight(1f),
            )
        }
        val usagePercentage = state.overview?.usagePercentage ?: 0.0
        if (usagePercentage >= 80.0) {
            Spacer(Modifier.height(12.dp))
            Card(Modifier.fillMaxWidth()) {
                Text(
                    text = stringResource(R.string.card_limit_warning),
                    modifier = Modifier.padding(16.dp),
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodyMedium,
                )
            }
        }
        Spacer(Modifier.height(12.dp))
        Text(
            text = "${stringResource(R.string.card_payment_account)}: ${card.paymentAccountName ?: stringResource(R.string.card_no_payment_account)}",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

@Composable
private fun SummaryCard(label: String, value: Double, modifier: Modifier) {
    Card(modifier) {
        Column(Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(4.dp))
            Text(Money.brl(value), style = MaterialTheme.typography.titleMedium)
        }
    }
}

@Composable
private fun EmptyText(text: String) {
    Text(
        text = text,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
        textAlign = TextAlign.Center,
        modifier = Modifier.fillMaxWidth().padding(top = 24.dp),
    )
}

@Composable
private fun PurchaseRow(purchase: CreditCardPurchase, onClick: () -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick).padding(vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(purchase.description, style = MaterialTheme.typography.bodyLarge)
            Text(
                text = "${formatDate(purchase.purchaseDate)} - ${purchase.categoryName.orEmpty()} - ${purchase.installments}x",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text(Money.brl(purchase.totalAmount), style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
private fun StatementRow(statement: CreditCardStatement, onClick: () -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick).padding(vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(statementReference(statement.referenceMonth, statement.referenceYear), style = MaterialTheme.typography.bodyLarge)
            Text(
                text = "${statementStatusLabel(statement.status)} - ${formatDate(statement.dueDate)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text(Money.brl(statement.openBalance), style = MaterialTheme.typography.bodyLarge)
    }
}
