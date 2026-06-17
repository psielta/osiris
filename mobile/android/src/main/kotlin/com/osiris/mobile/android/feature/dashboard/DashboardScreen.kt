package com.osiris.mobile.android.feature.dashboard

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.feature.cards.formatDate
import com.osiris.mobile.android.feature.cards.statementStatusLabel
import com.osiris.mobile.android.ui.components.parseHexColor
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.CreditCardDashboard
import com.osiris.mobile.domain.model.DashboardAlert
import com.osiris.mobile.domain.model.DashboardAlertSeverity
import com.osiris.mobile.domain.model.DashboardSummary
import com.osiris.mobile.domain.model.Onboarding
import com.osiris.mobile.domain.model.SpendingByCategory
import com.osiris.mobile.domain.model.UpcomingObligation
import com.osiris.mobile.presentation.dashboard.DashboardViewModel
import org.koin.androidx.compose.koinViewModel
import java.util.Locale
import kotlin.math.min

private const val MaxSpendingChartSlices = 8
private const val OtherSpendingColor = "#94A3B8"

private val SpendingFallbackPalette = listOf(
    "#F59E0B",
    "#10B981",
    "#0EA5E9",
    "#8B5CF6",
    "#F43F5E",
    "#14B8A6",
    "#EAB308",
    "#6366F1",
)

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
                OnboardingCard(summary.onboarding)
            }
        }

        items(summary.alerts, key = { it.message }) { alert ->
            AlertCard(alert)
        }

        item { SectionTitle(stringResource(R.string.dashboard_overview)) }
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

        item { SectionTitle(stringResource(R.string.dashboard_due_month)) }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(
                    label = stringResource(R.string.dashboard_bills_due),
                    value = Money.brl(summary.billsDueInMonthTotal),
                    modifier = Modifier.weight(1f),
                    supportingText = stringResource(
                        R.string.dashboard_bills_due_detail,
                        summary.billsDueInMonthCount,
                        Money.brl(summary.billsOpenInMonthTotal),
                    ),
                )
                MetricCard(
                    label = stringResource(R.string.dashboard_statements_due),
                    value = Money.brl(summary.statementsDueInMonthTotal),
                    modifier = Modifier.weight(1f),
                    supportingText = stringResource(
                        R.string.dashboard_statements_due_detail,
                        summary.statementsDueInMonthCount,
                        Money.brl(summary.statementsOpenInMonthTotal),
                    ),
                )
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
                MetricCard(
                    label = stringResource(R.string.dashboard_overdue_statements),
                    value = "${summary.overdueStatementsCount}",
                    modifier = Modifier.weight(1f),
                    supportingText = stringResource(R.string.dashboard_overdue_detail, Money.brl(summary.overdueStatementsBalance)),
                    isDanger = summary.overdueStatementsCount > 0,
                )
                MetricCard(
                    label = stringResource(R.string.dashboard_partial_statements),
                    value = "${summary.partiallyPaidStatementsCount}",
                    modifier = Modifier.weight(1f),
                    supportingText = stringResource(R.string.dashboard_partial_detail),
                    isWarning = summary.partiallyPaidStatementsCount > 0,
                )
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                MetricCard(stringResource(R.string.dashboard_statement_payments), Money.brl(summary.statementPaymentsInMonthTotal), Modifier.weight(1f))
                MetricCard(stringResource(R.string.dashboard_future_installments), Money.brl(summary.futureInstallmentsTotal), Modifier.weight(1f))
            }
        }

        item { SectionTitle(stringResource(R.string.dashboard_spending_view)) }
        item { SectionTitle(stringResource(R.string.dashboard_categories)) }
        if (summary.spendingByCategory.isEmpty()) {
            item { EmptyText(stringResource(R.string.dashboard_no_spending)) }
        } else {
            item { SpendingPieChart(summary.spendingByCategory) }
            itemsIndexed(summary.spendingByCategory, key = { _, category -> category.categoryId ?: category.categoryName }) { index, category ->
                CategoryRow(category, categoryChartColor(category, index))
            }
        }

        item { SectionTitle(stringResource(R.string.dashboard_cash_view)) }
        item { CashFlowBreakdown(summary) }

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
private fun MetricCard(
    label: String,
    value: String,
    modifier: Modifier,
    supportingText: String? = null,
    isDanger: Boolean = false,
    isWarning: Boolean = false,
) {
    val valueColor = when {
        isDanger -> MaterialTheme.colorScheme.error
        isWarning -> MaterialTheme.colorScheme.tertiary
        else -> MaterialTheme.colorScheme.onSurface
    }

    Card(modifier) {
        Column(Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(4.dp))
            Text(value, style = MaterialTheme.typography.titleMedium, color = valueColor)
            if (!supportingText.isNullOrBlank()) {
                Spacer(Modifier.height(4.dp))
                Text(
                    supportingText,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}

@Composable
private fun OnboardingCard(onboarding: Onboarding) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Text(stringResource(R.string.dashboard_onboarding_title), style = MaterialTheme.typography.titleSmall)
            Spacer(Modifier.height(4.dp))
            Text(
                stringResource(R.string.dashboard_onboarding_body),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            HorizontalDivider()
            OnboardingStep(onboarding.hasFinancialAccount, stringResource(R.string.dashboard_onboarding_financial_account))
            OnboardingStep(onboarding.hasCreditCard, stringResource(R.string.dashboard_onboarding_credit_card))
            OnboardingStep(onboarding.hasActiveCategories, stringResource(R.string.dashboard_onboarding_categories))
            OnboardingStep(onboarding.hasFirstSpending, stringResource(R.string.dashboard_onboarding_first_spending))
        }
    }
}

@Composable
private fun OnboardingStep(done: Boolean, label: String) {
    val color = if (done) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.outline
    val status = stringResource(if (done) R.string.dashboard_onboarding_done else R.string.dashboard_onboarding_pending)

    Row(
        Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Box(
            modifier = Modifier
                .size(10.dp)
                .background(color, CircleShape),
        )
        Text(label, modifier = Modifier.weight(1f), style = MaterialTheme.typography.bodySmall)
        Text(status, style = MaterialTheme.typography.bodySmall, color = color)
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
private fun SpendingPieChart(categories: List<SpendingByCategory>) {
    val otherLabel = stringResource(R.string.dashboard_spending_other)
    val slices = buildSpendingChartSlices(categories, otherLabel)
    val total = slices.sumOf { it.value }
    if (total <= 0.0 || slices.isEmpty()) {
        return
    }

    val trackColor = MaterialTheme.colorScheme.surfaceVariant

    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Text(stringResource(R.string.dashboard_spending_chart), style = MaterialTheme.typography.titleSmall)
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(220.dp),
                contentAlignment = Alignment.Center,
            ) {
                Canvas(Modifier.fillMaxSize().padding(10.dp)) {
                    val diameter = min(size.width, size.height) * 0.82f
                    val strokeWidth = diameter * 0.2f
                    val topLeft = androidx.compose.ui.geometry.Offset(
                        x = (size.width - diameter) / 2f,
                        y = (size.height - diameter) / 2f,
                    )
                    val arcSize = androidx.compose.ui.geometry.Size(diameter, diameter)

                    drawArc(
                        color = trackColor,
                        startAngle = 0f,
                        sweepAngle = 360f,
                        useCenter = false,
                        topLeft = topLeft,
                        size = arcSize,
                        style = androidx.compose.ui.graphics.drawscope.Stroke(width = strokeWidth),
                    )

                    var startAngle = -90f
                    slices.forEach { slice ->
                        val sweep = ((slice.value / total) * 360.0).toFloat()
                        drawArc(
                            color = slice.color,
                            startAngle = startAngle,
                            sweepAngle = sweep,
                            useCenter = false,
                            topLeft = topLeft,
                            size = arcSize,
                            style = androidx.compose.ui.graphics.drawscope.Stroke(width = strokeWidth),
                        )
                        startAngle += sweep
                    }
                }
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        stringResource(R.string.dashboard_spending_chart_total),
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Text(Money.brl(total), style = MaterialTheme.typography.titleMedium)
                }
            }
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                slices.forEach { slice ->
                    SpendingLegendRow(slice, total)
                }
            }
        }
    }
}

@Composable
private fun SpendingLegendRow(slice: SpendingChartSlice, total: Double) {
    Row(
        Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Box(
            modifier = Modifier
                .size(10.dp)
                .background(slice.color, CircleShape),
        )
        Text(slice.label, modifier = Modifier.weight(1f), style = MaterialTheme.typography.bodySmall)
        Text(
            "${Money.brl(slice.value)} - ${formatPercent((slice.value / total) * 100.0)}",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

@Composable
private fun CategoryRow(category: SpendingByCategory, color: Color) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(10.dp)
                        .background(color, CircleShape),
                )
                Text(category.categoryName, modifier = Modifier.weight(1f), style = MaterialTheme.typography.bodyLarge)
                Text(Money.brl(category.total), style = MaterialTheme.typography.titleSmall)
            }
            Spacer(Modifier.height(8.dp))
            AmountRow(stringResource(R.string.dashboard_card_purchases), Money.brl(category.cardPurchasesTotal))
            AmountRow(stringResource(R.string.dashboard_bills_spending), Money.brl(category.billsTotal))
            AmountRow(stringResource(R.string.dashboard_direct_expenses), Money.brl(category.directExpensesTotal))
        }
    }
}

private data class SpendingChartSlice(
    val label: String,
    val value: Double,
    val color: Color,
)

private fun buildSpendingChartSlices(
    categories: List<SpendingByCategory>,
    otherLabel: String,
): List<SpendingChartSlice> {
    val positive = categories
        .filter { it.total > 0.0 }
        .sortedByDescending { it.total }

    val slices = positive
        .take(MaxSpendingChartSlices)
        .mapIndexed { index, category ->
            SpendingChartSlice(category.categoryName, category.total, categoryChartColor(category, index))
        }

    if (positive.size <= MaxSpendingChartSlices) {
        return slices
    }

    val othersTotal = positive.drop(MaxSpendingChartSlices).sumOf { it.total }
    return slices + SpendingChartSlice(otherLabel, othersTotal, chartColor(OtherSpendingColor, 0))
}

private fun categoryChartColor(category: SpendingByCategory, index: Int): Color =
    chartColor(category.categoryColor, index)

private fun chartColor(hex: String?, index: Int): Color {
    val fallback = SpendingFallbackPalette[index % SpendingFallbackPalette.size]
    return hex
        ?.takeIf { it.isNotBlank() }
        ?.let { parseHexColor(it) }
        ?: parseHexColor(fallback)
        ?: Color(0xFFF59E0B)
}

private fun formatPercent(value: Double): String =
    String.format(Locale("pt", "BR"), "%.1f%%", value)

@Composable
private fun CashFlowBreakdown(summary: DashboardSummary) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            AmountRow(
                label = stringResource(R.string.dashboard_cash_income_received),
                value = "+ ${Money.brl(summary.cashFlow.incomeTotal)}",
                valueColor = MaterialTheme.colorScheme.primary,
            )
            AmountRow(stringResource(R.string.dashboard_cash_bills_paid), "- ${Money.brl(summary.cashFlow.billsPaidTotal)}")
            AmountRow(stringResource(R.string.dashboard_cash_statement_payments), "- ${Money.brl(summary.cashFlow.statementPaymentsTotal)}")
            AmountRow(stringResource(R.string.dashboard_cash_direct_expenses), "- ${Money.brl(summary.cashFlow.directExpensesTotal)}")
            HorizontalDivider()
            AmountRow(
                label = stringResource(R.string.dashboard_cash_bills_open),
                value = Money.brl(summary.cashFlow.billsOpenInMonthTotal),
                valueColor = MaterialTheme.colorScheme.tertiary,
            )
            AmountRow(
                label = stringResource(R.string.dashboard_cash_statements_open),
                value = Money.brl(summary.cashFlow.statementsOpenInMonthTotal),
                valueColor = MaterialTheme.colorScheme.tertiary,
            )
            HorizontalDivider()
            AmountRow(
                label = stringResource(R.string.dashboard_cash_projected_after_all),
                value = Money.brl(summary.cashFlow.projectedCashBalance),
                valueColor = if (summary.cashFlow.projectedCashBalance < 0) {
                    MaterialTheme.colorScheme.error
                } else {
                    MaterialTheme.colorScheme.primary
                },
            )
        }
    }
}

@Composable
private fun AmountRow(
    label: String,
    value: String,
    valueColor: Color? = null,
) {
    val resolvedValueColor = valueColor ?: MaterialTheme.colorScheme.onSurface

    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(label, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.bodySmall, color = resolvedValueColor)
    }
}

@Composable
private fun CreditCardRiskRow(card: CreditCardDashboard) {
    val usageColor = if (card.usagePercentage >= 80) {
        MaterialTheme.colorScheme.error
    } else if (card.usagePercentage >= 50) {
        MaterialTheme.colorScheme.tertiary
    } else {
        MaterialTheme.colorScheme.primary
    }
    val usageProgress = (card.usagePercentage / 100.0).toFloat().coerceIn(0f, 1f)
    val status = card.currentStatementStatus?.let { statementStatusLabel(it) }
    val dueDate = card.currentStatementDueDate?.let { formatDate(it) }

    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(card.name, style = MaterialTheme.typography.bodyLarge)
                Text("${card.usagePercentage.toInt()}%", color = usageColor)
            }
            LinearProgressIndicator(
                progress = { usageProgress },
                modifier = Modifier.fillMaxWidth().height(6.dp),
                color = usageColor,
                trackColor = MaterialTheme.colorScheme.surfaceVariant,
            )
            AmountRow(stringResource(R.string.card_limit_label), Money.brl(card.limit))
            AmountRow(stringResource(R.string.card_used_limit), Money.brl(card.usedLimit), usageColor)
            AmountRow(stringResource(R.string.card_available_limit), Money.brl(card.availableLimit))
            HorizontalDivider()
            AmountRow(stringResource(R.string.card_current_statement), Money.brl(card.currentStatementTotal))
            AmountRow(
                label = stringResource(R.string.statement_open_balance),
                value = Money.brl(card.currentStatementOpenBalance),
                valueColor = if (card.currentStatementOpenBalance > 0) MaterialTheme.colorScheme.tertiary else MaterialTheme.colorScheme.onSurface,
            )
            if (status != null || dueDate != null) {
                Text(
                    listOfNotNull(status, dueDate?.let { "${stringResource(R.string.statement_due_date)} $it" }).joinToString(" - "),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            AmountRow(stringResource(R.string.card_future_installments), Money.brl(card.futureInstallmentsTotal))
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
                    formatDate(obligation.dueDate),
                    style = MaterialTheme.typography.bodySmall,
                    color = if (obligation.isOverdue) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Text(Money.brl(obligation.amount), style = MaterialTheme.typography.bodyLarge)
        }
    }
}
