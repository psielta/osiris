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
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.android.ui.components.OsirisTextField
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.presentation.cards.CardFormEvent
import com.osiris.mobile.presentation.cards.CardFormViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CardFormScreen(
    cardId: String?,
    onDone: () -> Unit,
    viewModel: CardFormViewModel = koinViewModel { parametersOf(cardId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                CardFormEvent.NavigateBack -> onDone()
                is CardFormEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    val noAccount = stringResource(R.string.card_no_payment_account)
    val accountOptions = remember(state.paymentAccounts) { listOf<Account?>(null) + state.paymentAccounts }
    val selectedAccount = state.paymentAccounts.firstOrNull { it.id == state.paymentAccountId }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(if (state.isEdit) R.string.card_edit else R.string.card_new)) },
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
                    value = state.name,
                    onValueChange = viewModel::onNameChange,
                    label = stringResource(R.string.card_name_label),
                    error = state.nameError,
                )
                Spacer(Modifier.height(20.dp))
                OsirisTextField(
                    value = state.limit,
                    onValueChange = viewModel::onLimitChange,
                    label = stringResource(R.string.card_limit_label),
                    error = state.limitError,
                    keyboardType = KeyboardType.Decimal,
                )
                Spacer(Modifier.height(20.dp))
                OsirisTextField(
                    value = state.closingDay,
                    onValueChange = viewModel::onClosingDayChange,
                    label = stringResource(R.string.card_closing_day_label),
                    error = state.closingDayError,
                    keyboardType = KeyboardType.Number,
                )
                Spacer(Modifier.height(20.dp))
                OsirisTextField(
                    value = state.dueDay,
                    onValueChange = viewModel::onDueDayChange,
                    label = stringResource(R.string.card_due_day_label),
                    error = state.dueDayError,
                    keyboardType = KeyboardType.Number,
                )
                Spacer(Modifier.height(20.dp))
                OsirisDropdownField(
                    label = stringResource(R.string.card_payment_account_label),
                    selected = selectedAccount,
                    options = accountOptions,
                    optionLabel = { it?.name ?: noAccount },
                    onSelect = { viewModel.onPaymentAccountChange(it?.id) },
                )
                if (state.paymentAccountError != null) {
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = state.paymentAccountError!!,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
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
                        Text(stringResource(R.string.card_save))
                    }
                }
            }
        }
    }
}
