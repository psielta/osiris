package com.osiris.mobile.android.feature.cards

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
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.CreditCardStatementInstallment
import com.osiris.mobile.domain.model.CreditCardStatementPayment
import com.osiris.mobile.presentation.cards.StatementDetailsEvent
import com.osiris.mobile.presentation.cards.StatementDetailsViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StatementDetailsScreen(
    cardId: String,
    statementId: String,
    onNavigateBack: () -> Unit,
    onPay: () -> Unit,
    viewModel: StatementDetailsViewModel = koinViewModel { parametersOf(cardId, statementId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val context = LocalContext.current

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is StatementDetailsEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
                is StatementDetailsEvent.OpenPdf -> openStatementPdf(context, event.pdf)
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    val statement = state.statement
                    Text(
                        if (statement == null) {
                            stringResource(R.string.statement_details_card)
                        } else {
                            statementReference(statement.referenceMonth, statement.referenceYear)
                        },
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
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

            state.statement != null -> {
                val statement = state.statement!!
                LazyColumn(
                    modifier = Modifier.fillMaxSize().padding(padding),
                    contentPadding = PaddingValues(16.dp),
                ) {
                    item {
                        Summary(statement.totalAmount, statement.paidAmount, statement.openBalance)
                        Spacer(Modifier.height(12.dp))
                        Text(
                            text = "${stringResource(R.string.statement_closing_date)}: ${formatDate(statement.closingDate)}",
                            style = MaterialTheme.typography.bodyMedium,
                        )
                        Text(
                            text = "${stringResource(R.string.statement_due_date)}: ${formatDate(statement.dueDate)}",
                            style = MaterialTheme.typography.bodyMedium,
                        )
                        Text(
                            text = statementStatusLabel(statement.status),
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.primary,
                        )
                        Spacer(Modifier.height(16.dp))
                        Row(horizontalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.fillMaxWidth()) {
                            if (statement.openBalance > 0.0) {
                                Button(onClick = onPay, modifier = Modifier.weight(1f)) {
                                    Icon(Icons.Filled.Add, contentDescription = null)
                                    Spacer(Modifier.size(8.dp))
                                    Text(stringResource(R.string.statement_pay))
                                }
                            }
                            OutlinedButton(
                                onClick = viewModel::downloadPdf,
                                enabled = !state.isDownloadingPdf,
                                modifier = Modifier.weight(1f),
                            ) {
                                if (state.isDownloadingPdf) {
                                    CircularProgressIndicator(modifier = Modifier.size(18.dp), strokeWidth = 2.dp)
                                } else {
                                    Icon(Icons.Filled.MoreVert, contentDescription = null)
                                    Spacer(Modifier.size(8.dp))
                                    Text(stringResource(R.string.statement_pdf))
                                }
                            }
                        }
                        Spacer(Modifier.height(24.dp))
                        Text(stringResource(R.string.statement_installments), style = MaterialTheme.typography.titleMedium)
                    }
                    items(statement.installmentItems, key = { it.id }) { installment ->
                        StatementInstallmentRow(installment)
                        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                    }
                    item {
                        Spacer(Modifier.height(24.dp))
                        Text(stringResource(R.string.statement_payments), style = MaterialTheme.typography.titleMedium)
                        if (statement.payments.isEmpty()) {
                            Spacer(Modifier.height(8.dp))
                            Text(
                                stringResource(R.string.statement_no_payments),
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                        }
                    }
                    items(statement.payments, key = { it.id }) { payment ->
                        PaymentRow(payment)
                        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                    }
                }
            }
        }
    }
}

@Composable
private fun Summary(total: Double, paid: Double, open: Double) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        SummaryCard(stringResource(R.string.statement_total), total, Modifier.weight(1f))
        SummaryCard(stringResource(R.string.statement_open_balance), open, Modifier.weight(1f))
    }
    Spacer(Modifier.height(12.dp))
    SummaryCard(stringResource(R.string.statement_paid), paid, Modifier.fillMaxWidth())
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
private fun StatementInstallmentRow(installment: CreditCardStatementInstallment) {
    Row(Modifier.fillMaxWidth().padding(vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
        Column(Modifier.weight(1f)) {
            Text(installment.purchaseDescription, style = MaterialTheme.typography.bodyLarge)
            Text(
                "${installment.installmentNumber}/${installment.totalInstallments}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text(Money.brl(installment.amount), style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
private fun PaymentRow(payment: CreditCardStatementPayment) {
    Row(Modifier.fillMaxWidth().padding(vertical = 12.dp), verticalAlignment = Alignment.CenterVertically) {
        Column(Modifier.weight(1f)) {
            Text(formatDate(payment.paidAt), style = MaterialTheme.typography.bodyLarge)
            Text(
                payment.financialAccountName ?: stringResource(R.string.payment_no_account),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text(Money.brl(payment.amount), style = MaterialTheme.typography.bodyLarge)
    }
}
