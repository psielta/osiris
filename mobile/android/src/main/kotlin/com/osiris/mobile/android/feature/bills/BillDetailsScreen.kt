package com.osiris.mobile.android.feature.bills

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
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
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
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDateField
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.android.ui.components.RefreshOnResume
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.BillStatus
import com.osiris.mobile.presentation.bills.BillDetailsEvent
import com.osiris.mobile.presentation.bills.BillDetailsViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BillDetailsScreen(
    billId: String,
    onNavigateBack: () -> Unit,
    onEdit: () -> Unit,
    viewModel: BillDetailsViewModel = koinViewModel { parametersOf(billId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var confirmDelete by remember { mutableStateOf(false) }

    RefreshOnResume { viewModel.load() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                BillDetailsEvent.NavigateBack -> onNavigateBack()
                is BillDetailsEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.bill?.description ?: stringResource(R.string.bill_details)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
                actions = {
                    IconButton(onClick = onEdit) {
                        Icon(Icons.Filled.Edit, contentDescription = stringResource(R.string.bill_edit))
                    }
                    IconButton(onClick = { confirmDelete = true }) {
                        Icon(Icons.Filled.Delete, contentDescription = stringResource(R.string.bill_delete))
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

            state.bill != null -> {
                val bill = state.bill!!
                val selectedAccount = state.accounts.firstOrNull { it.id == state.paymentAccountId }
                val accountOptions = remember(state.accounts) { listOf<Account?>(null) + state.accounts }
                val noAccountLabel = stringResource(R.string.payment_no_account)
                LazyColumn(
                    modifier = Modifier.fillMaxSize().padding(padding),
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    item {
                        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                            SummaryCard(stringResource(R.string.bill_amount_label), Money.brl(bill.amount), Modifier.weight(1f))
                            SummaryCard(stringResource(R.string.bill_status_label), billStatusLabel(bill.status), Modifier.weight(1f))
                        }
                    }
                    item {
                        Card(Modifier.fillMaxWidth()) {
                            Column(Modifier.padding(16.dp)) {
                                Text("${stringResource(R.string.bill_due_date_label)}: ${formatDate(bill.dueDate)}")
                                Text("${stringResource(R.string.bill_category_label)}: ${bill.categoryName.orEmpty()}")
                                Text("${stringResource(R.string.bill_payment_account_label)}: ${bill.paymentAccountName ?: stringResource(R.string.payment_no_account)}")
                                val notes = bill.notes
                                if (!notes.isNullOrBlank()) {
                                    Spacer(Modifier.height(8.dp))
                                    Text(notes, color = MaterialTheme.colorScheme.onSurfaceVariant)
                                }
                            }
                        }
                    }
                    if (bill.status == BillStatus.Paid) {
                        item {
                            OutlinedButton(
                                onClick = viewModel::markPending,
                                enabled = !state.isUpdating,
                                modifier = Modifier.fillMaxWidth(),
                            ) {
                                Text(stringResource(R.string.bill_mark_pending))
                            }
                        }
                    } else {
                        item {
                            Card(Modifier.fillMaxWidth()) {
                                Column(Modifier.padding(16.dp)) {
                                    Text(stringResource(R.string.bill_pay), style = MaterialTheme.typography.titleSmall)
                                    Spacer(Modifier.height(12.dp))
                                    OsirisDateField(
                                        label = stringResource(R.string.payment_date_label),
                                        value = state.paidAt,
                                        onValueChange = viewModel::onPaidAtChange,
                                    )
                                    Spacer(Modifier.height(12.dp))
                                    OsirisDropdownField(
                                        label = stringResource(R.string.payment_account_label),
                                        selected = selectedAccount,
                                        options = accountOptions,
                                        optionLabel = { it?.name ?: noAccountLabel },
                                        onSelect = { viewModel.onPaymentAccountChange(it?.id) },
                                    )
                                    Spacer(Modifier.height(16.dp))
                                    Button(
                                        onClick = viewModel::pay,
                                        enabled = !state.isUpdating,
                                        modifier = Modifier.fillMaxWidth(),
                                    ) {
                                        if (state.isUpdating) {
                                            CircularProgressIndicator(
                                                modifier = Modifier.size(20.dp),
                                                strokeWidth = 2.dp,
                                                color = MaterialTheme.colorScheme.onPrimary,
                                            )
                                        } else {
                                            Text(stringResource(R.string.bill_mark_paid))
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if (confirmDelete) {
        AlertDialog(
            onDismissRequest = { confirmDelete = false },
            text = { Text(stringResource(R.string.bill_delete_confirm)) },
            confirmButton = {
                TextButton(onClick = {
                    confirmDelete = false
                    viewModel.delete()
                }) { Text(stringResource(R.string.bill_delete)) }
            },
            dismissButton = { TextButton(onClick = { confirmDelete = false }) { Text(stringResource(R.string.cancel)) } },
        )
    }
}

@Composable
private fun SummaryCard(label: String, value: String, modifier: Modifier) {
    Card(modifier) {
        Column(Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(4.dp))
            Text(value, style = MaterialTheme.typography.titleMedium)
        }
    }
}
