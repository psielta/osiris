package com.osiris.mobile.android.feature.dashboard

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
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.CreditCardDashboard
import com.osiris.mobile.domain.model.DashboardAlert
import com.osiris.mobile.domain.model.DashboardAlertSeverity
import com.osiris.mobile.domain.model.DashboardSummary
import com.osiris.mobile.domain.model.SpendingByCategory
import com.osiris.mobile.domain.model.UpcomingObligation
import com.osiris.mobile.presentation.dashboard.DashboardViewModel
import org.koin.androidx.compose.koinViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    onNavigateBack: () -> Unit,
    showBackButton: Boolean = true,
    viewModel: DashboardViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.dashboard_title)) },
                navigationIcon = {
                    if (showBackButton) {
                        IconButton(onClick = onNavigateBack) {
                            Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                        }
                    }
                },
            )
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

            state.summary != null -> DashboardContent(
                summary = state.summary!!,
                month = state.month,
                year = state.year,
                onPrevious = viewModel::previousMonth,
                onNext = viewModel::nextMonth,
                modifier = Modifier.padding(padding),
            )
        }
    }
}

@Composable
private fun DashboardContent(
    summary: DashboardSummary,
    month: Int,
    year: Int,
    onPrevious: () -> Unit,
    onNext: () -> Unit,
    modifier: Modifier = Modifier,
) {
    LazyColumn(
        modifier = modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedButton(onClick = onPrevious, modifier = Modifier.weight(1f)) { Text("<") }
                FilledTonalButton(onClick = {}, modifier = Modifier.weight(2f), enabled = false) {
                    Text("${month.toString().padStart(2, '0')}/$year")
                }
                OutlinedButton(onClick = onNext, modifier = Modifier.weight(1f)) { Text(">") }
            }
        }

        if (!summary.onboarding.isComplete) {
            item {
                InfoCard(
                    title = stringResource(R.string.dashboard_onboarding_title),
                    body = stringResource(R.string.dashboard_onboarding_body),
                )
            }
        }

        items(summary.alerts, key = { it.message }) { alert ->
            AlertCard(alert)
        }

        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_income), Money.brl(summary.incomeTotal), Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_spending), Money.brl(summary.spendingTotal), Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_projected_cash), Money.brl(summary.cashFlow.projectedCashBalance), Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_accounts_balance), Money.brl(summary.cashFlow.totalAccountsBalance), Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_open_statements), Money.brl(summary.totalOpenStatementsBalance), Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_open_bills), Money.brl(summary.totalOpenBillsBalance), Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_statement_payments), Money.brl(summary.statementPaymentsInMonthTotal), Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_future_installments), Money.brl(summary.futureInstallmentsTotal), Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_overdue_statements), "${summary.overdueStatementsCount}", Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_partial_statements), "${summary.partiallyPaidStatementsCount}", Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_bills_due), "${summary.billsDueInMonthCount}", Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_statements_due), "${summary.statementsDueInMonthCount}", Modifier.weight(1f))
            }
        }

        item { SectionTitle(stringResource(R.string.dashboard_categories)) }
        if (summary.spendingByCategory.isEmpty()) {
            item { EmptyText(stringResource(R.string.dashboard_no_spending)) }
        } else {
            items(summary.spendingByCategory, key = { it.categoryId ?: it.categoryName }) { category ->
                CategoryRow(category)
            }
        }

        item { SectionTitle(stringResource(R.string.dashboard_cards_risk)) }
        if (summary.creditCards.isEmpty()) {
            item { EmptyText(stringResource(R.string.cards_empty)) }
        } else {
            items(summary.creditCards, key = { it.creditCardId }) { card ->
                CreditCardRiskRow(card)
            }
        }

        item { SectionTitle(stringResource(R.string.dashboard_upcoming)) }
        if (summary.upcomingObligations.isEmpty()) {
            item { EmptyText(stringResource(R.string.dashboard_no_obligations)) }
        } else {
            items(summary.upcomingObligations, key = { it.id }) { obligation ->
                ObligationRow(obligation)
            }
        }
    }
}

@Composable
private fun MetricCard(label: String, value: String, modifier: Modifier) {
    Card(modifier) {
        Column(Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(4.dp))
            Text(value, style = MaterialTheme.typography.titleMedium)
        }
    }
}

@Composable
private fun InfoCard(title: String, body: String) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Text(title, style = MaterialTheme.typography.titleSmall)
            Spacer(Modifier.height(4.dp))
            Text(body, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}

@Composable
private fun AlertCard(alert: DashboardAlert) {
    val color = when (alert.severity) {
        DashboardAlertSeverity.Danger -> MaterialTheme.colorScheme.error
        DashboardAlertSeverity.Warning -> MaterialTheme.colorScheme.tertiary
        DashboardAlertSeverity.Info -> MaterialTheme.colorScheme.primary
    }
    Card(Modifier.fillMaxWidth()) {
        Text(
            text = alert.message,
            modifier = Modifier.padding(16.dp),
            style = MaterialTheme.typography.bodyMedium,
            color = color,
        )
    }
}

@Composable
private fun SectionTitle(text: String) {
    Text(
        text = text,
        style = MaterialTheme.typography.titleMedium,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(top = 8.dp),
    )
}

@Composable
private fun EmptyText(text: String) {
    Text(text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
}

@Composable
private fun CategoryRow(category: SpendingByCategory) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Text(category.categoryName, style = MaterialTheme.typography.bodyLarge)
            Text(
                text = Money.brl(category.total),
                style = MaterialTheme.typography.titleSmall,
            )
            Text(
                text = "${stringResource(R.string.dashboard_card_purchases)} ${Money.brl(category.cardPurchasesTotal)} - ${stringResource(R.string.bills_title)} ${Money.brl(category.billsTotal)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun CreditCardRiskRow(card: CreditCardDashboard) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(card.name, style = MaterialTheme.typography.bodyLarge)
                Text("${card.usagePercentage.toInt()}%", color = if (card.usagePercentage >= 80) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurface)
            }
            Spacer(Modifier.height(4.dp))
            Text(
                text = "${stringResource(R.string.card_used_limit)} ${Money.brl(card.usedLimit)} - ${stringResource(R.string.card_available_limit)} ${Money.brl(card.availableLimit)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Text(
                text = "${stringResource(R.string.card_future_installments)} ${Money.brl(card.futureInstallmentsTotal)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun ObligationRow(obligation: UpcomingObligation) {
    Card(Modifier.fillMaxWidth()) {
        Row(Modifier.padding(16.dp), verticalAlignment = Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {
                Text(obligation.description, style = MaterialTheme.typography.bodyLarge)
                Text(
                    obligation.dueDate,
                    style = MaterialTheme.typography.bodySmall,
                    color = if (obligation.isOverdue) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Text(Money.brl(obligation.amount), style = MaterialTheme.typography.bodyLarge)
        }
    }
}
