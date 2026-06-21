package com.osiris.mobile.android.feature.accounts

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
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.openPdf
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.model.Movement
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.presentation.accounts.AccountStatementEvent
import com.osiris.mobile.presentation.accounts.AccountStatementViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf
import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val inflowColor = Color(0xFF16A34A)
private val dateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountStatementScreen(
    accountId: String,
    onAddMovement: () -> Unit,
    onImport: () -> Unit,
    onImportCsv: () -> Unit,
    onImportPdf: () -> Unit,
    onNavigateBack: () -> Unit,
    viewModel: AccountStatementViewModel = koinViewModel { parametersOf(accountId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val context = LocalContext.current

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is AccountStatementEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
                is AccountStatementEvent.OpenPdf -> openPdf(context, event.pdf)
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.statement?.name ?: stringResource(R.string.statement_title)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
                actions = {
                    if (state.statement?.isActive == true) {
                        var importMenuOpen by remember { mutableStateOf(false) }
                        TextButton(onClick = { importMenuOpen = true }) {
                            Text(stringResource(R.string.statement_import_ofx))
                        }
                        DropdownMenu(expanded = importMenuOpen, onDismissRequest = { importMenuOpen = false }) {
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.import_ofx_menu)) },
                                onClick = {
                                    importMenuOpen = false
                                    onImport()
                                },
                            )
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.statement_import_csv)) },
                                onClick = {
                                    importMenuOpen = false
                                    onImportCsv()
                                },
                            )
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.statement_import_pdf)) },
                                onClick = {
                                    importMenuOpen = false
                                    onImportPdf()
                                },
                            )
                        }
                    }
                    if (state.statement != null) {
                        IconButton(onClick = viewModel::downloadPdf, enabled = !state.isDownloadingPdf) {
                            if (state.isDownloadingPdf) {
                                CircularProgressIndicator(modifier = Modifier.size(18.dp), strokeWidth = 2.dp)
                            } else {
                                Icon(Icons.Filled.MoreVert, contentDescription = stringResource(R.string.statement_pdf))
                            }
                        }
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
        floatingActionButton = {
            if (state.statement?.isActive == true) {
                ExtendedFloatingActionButton(
                    onClick = onAddMovement,
                    icon = { Icon(Icons.Filled.Add, contentDescription = null) },
                    text = { Text(stringResource(R.string.movement_new)) },
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

            else -> state.statement?.let { statement ->
                val rows = remember(statement) { runningBalances(statement) }
                LazyColumn(
                    modifier = Modifier.fillMaxSize().padding(padding),
                    contentPadding = PaddingValues(16.dp),
                ) {
                    item { SummaryCards(statement.initialBalance, statement.currentBalance) }
                    item { Spacer(Modifier.height(16.dp)) }
                    if (rows.isEmpty()) {
                        item {
                            Text(
                                text = stringResource(R.string.statement_empty),
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                textAlign = TextAlign.Center,
                                modifier = Modifier.fillMaxWidth().padding(top = 24.dp),
                            )
                        }
                    } else {
                        items(rows, key = { it.movement.id }) { row ->
                            MovementRow(row)
                            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
                        }
                    }
                }
            }
        }
    }
}

private data class MovementRowData(val movement: Movement, val runningBalance: Double)

/**
 * Computes the running balance per movement. The API returns movements newest-first; we accumulate in
 * chronological (oldest-first) order from the initial balance, then display newest-first.
 */
private fun runningBalances(statement: AccountStatement): List<MovementRowData> {
    val chronological = statement.movements.asReversed().sortedBy { it.occurredOn }
    var balance = statement.initialBalance
    val rows = chronological.map { movement ->
        balance += if (movement.isInflow) movement.amount else -movement.amount
        MovementRowData(movement, balance)
    }
    return rows.asReversed()
}

@Composable
private fun SummaryCards(initialBalance: Double, currentBalance: Double) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        SummaryCard(stringResource(R.string.statement_initial_balance), initialBalance, Modifier.weight(1f))
        SummaryCard(stringResource(R.string.statement_current_balance), currentBalance, Modifier.weight(1f))
    }
}

@Composable
private fun SummaryCard(label: String, value: Double, modifier: Modifier) {
    Card(modifier) {
        Column(Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(4.dp))
            Text(
                text = Money.brl(value),
                style = MaterialTheme.typography.titleMedium,
                color = if (value < 0) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurface,
            )
        }
    }
}

@Composable
private fun MovementRow(row: MovementRowData) {
    val movement = row.movement
    Row(Modifier.fillMaxWidth().padding(vertical = 12.dp)) {
        Column(Modifier.weight(1f)) {
            Text(movement.description, style = MaterialTheme.typography.bodyLarge)
            Text(
                text = "${formatDate(movement.occurredOn)} · ${movementTypeLabel(movement.type)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Column(horizontalAlignment = Alignment.End) {
            val sign = if (movement.isInflow) "+" else "−"
            Text(
                text = "$sign${Money.brl(movement.amount)}",
                style = MaterialTheme.typography.bodyLarge,
                color = if (movement.isInflow) inflowColor else MaterialTheme.colorScheme.error,
            )
            Text(
                text = Money.brl(row.runningBalance),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

private fun formatDate(iso: String): String =
    runCatching { LocalDate.parse(iso).format(dateFormat) }.getOrDefault(iso)

@Composable
private fun movementTypeLabel(type: MovementType): String = stringResource(
    when (type) {
        MovementType.Income -> R.string.movement_type_income
        MovementType.Expense -> R.string.movement_type_expense
        MovementType.BillPayment -> R.string.movement_type_bill
        MovementType.CreditCardStatementPayment -> R.string.movement_type_card
        MovementType.TransferIn -> R.string.movement_type_transfer_in
        MovementType.TransferOut -> R.string.movement_type_transfer_out
        MovementType.Adjustment -> R.string.movement_type_adjustment
    },
)
