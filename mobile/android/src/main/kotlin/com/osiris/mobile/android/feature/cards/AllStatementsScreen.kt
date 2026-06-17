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
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.RefreshOnResume
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.CreditCardStatementOverview
import com.osiris.mobile.domain.model.StatementStatus
import com.osiris.mobile.presentation.cards.AllStatementsViewModel
import org.koin.androidx.compose.koinViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AllStatementsScreen(
    onNavigateBack: () -> Unit,
    onOpenStatement: (String, String) -> Unit,
    viewModel: AllStatementsViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    RefreshOnResume { viewModel.load() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.statements_title)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
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

            else -> LazyColumn(
                modifier = Modifier.fillMaxSize().padding(padding),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                item {
                    DateRangeFilterControls(
                        title = stringResource(R.string.filter_statement_due_date),
                        range = state.range,
                        filterError = state.filterError,
                        onCurrentMonth = viewModel::selectCurrentMonth,
                        onNextMonth = viewModel::selectNextMonth,
                        onFromChange = viewModel::onCustomFromChange,
                        onToChange = viewModel::onCustomToChange,
                        onApply = viewModel::applyCustomRange,
                    )
                }
                if (state.statements.isEmpty()) {
                    item {
                        Text(
                            text = stringResource(R.string.statements_empty_period),
                            modifier = Modifier.fillMaxWidth().padding(24.dp),
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            textAlign = TextAlign.Center,
                        )
                    }
                } else {
                    items(state.statements, key = { it.id }) { statement ->
                        StatementRow(statement, onClick = { onOpenStatement(statement.creditCardId, statement.id) })
                    }
                }
            }
        }
    }
}

@Composable
private fun StatementRow(statement: CreditCardStatementOverview, onClick: () -> Unit) {
    val statusColor = when (statement.status) {
        StatementStatus.Overdue -> MaterialTheme.colorScheme.error
        StatementStatus.PartiallyPaid -> MaterialTheme.colorScheme.tertiary
        StatementStatus.Paid -> MaterialTheme.colorScheme.primary
        StatementStatus.Closed,
        StatementStatus.Open -> MaterialTheme.colorScheme.onSurfaceVariant
    }
    Card(Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Row(Modifier.padding(16.dp), verticalAlignment = Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {
                Text(statement.creditCardName, style = MaterialTheme.typography.bodyLarge)
                Text(
                    text = "${statementReference(statement.referenceMonth, statement.referenceYear)} - ${formatDate(statement.dueDate)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Text(statementStatusLabel(statement.status), style = MaterialTheme.typography.bodySmall, color = statusColor)
            }
            Column(horizontalAlignment = Alignment.End) {
                Text(Money.brl(statement.openBalance), style = MaterialTheme.typography.bodyLarge)
                Text(Money.brl(statement.totalAmount), style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}
