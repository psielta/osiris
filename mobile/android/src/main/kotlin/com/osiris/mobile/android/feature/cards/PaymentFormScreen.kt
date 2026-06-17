package com.osiris.mobile.android.feature.cards

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDateField
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.android.ui.components.OsirisTextField
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.presentation.cards.PaymentFormEvent
import com.osiris.mobile.presentation.cards.PaymentFormViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PaymentFormScreen(
    cardId: String,
    statementId: String,
    onDone: () -> Unit,
    viewModel: PaymentFormViewModel = koinViewModel { parametersOf(cardId, statementId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                PaymentFormEvent.NavigateBack -> onDone()
                is PaymentFormEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    val noAccount = stringResource(R.string.payment_no_account)
    val accountOptions = remember(state.accounts) { listOf<Account?>(null) + state.accounts }
    val selectedAccount = state.accounts.firstOrNull { it.id == state.financialAccountId }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.payment_new)) },
                navigationIcon = {
                    IconButton(onClick = onDone) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        if (state.isLoading) {
            Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .padding(24.dp)
                    .verticalScroll(rememberScrollState()),
            ) {
                OsirisTextField(
                    value = state.amount,
                    onValueChange = viewModel::onAmountChange,
                    label = stringResource(R.string.payment_amount_label),
                    error = state.amountError,
                    keyboardType = KeyboardType.Decimal,
                )
                Spacer(Modifier.height(20.dp))
                OsirisDateField(
                    label = stringResource(R.string.payment_date_label),
                    value = state.paidAt,
                    onValueChange = viewModel::onPaidAtChange,
                )
                if (state.paidAtError != null) {
                    Spacer(Modifier.height(4.dp))
                    Text(state.paidAtError!!, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                }
                Spacer(Modifier.height(20.dp))
                OsirisDropdownField(
                    label = stringResource(R.string.payment_account_label),
                    selected = selectedAccount,
                    options = accountOptions,
                    optionLabel = { it?.name ?: noAccount },
                    onSelect = { viewModel.onFinancialAccountChange(it?.id) },
                )
                if (state.financialAccountError != null) {
                    Spacer(Modifier.height(4.dp))
                    Text(state.financialAccountError!!, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                }
                Spacer(Modifier.height(20.dp))
                OsirisTextField(
                    value = state.notes,
                    onValueChange = viewModel::onNotesChange,
                    label = stringResource(R.string.payment_notes_label),
                    error = state.notesError,
                )
                Spacer(Modifier.height(32.dp))
                Button(
                    onClick = viewModel::submit,
                    enabled = !state.isSubmitting,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    if (state.isSubmitting) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(20.dp),
                            strokeWidth = 2.dp,
                            color = MaterialTheme.colorScheme.onPrimary,
                        )
                    } else {
                        Text(stringResource(R.string.payment_save))
                    }
                }
            }
        }
    }
}
