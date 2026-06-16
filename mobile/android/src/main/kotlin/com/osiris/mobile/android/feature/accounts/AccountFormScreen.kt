package com.osiris.mobile.android.feature.accounts

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
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.presentation.accounts.AccountFormEvent
import com.osiris.mobile.presentation.accounts.AccountFormViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountFormScreen(
    accountId: String?,
    onDone: () -> Unit,
    viewModel: AccountFormViewModel = koinViewModel { parametersOf(accountId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                AccountFormEvent.NavigateBack -> onDone()
                is AccountFormEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    val typeLabels = mapOf(
        AccountType.Checking to stringResource(R.string.account_type_checking),
        AccountType.Savings to stringResource(R.string.account_type_savings),
        AccountType.Cash to stringResource(R.string.account_type_cash),
        AccountType.Other to stringResource(R.string.account_type_other),
    )

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(if (state.isEdit) R.string.account_edit else R.string.account_new)) },
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
                    label = stringResource(R.string.account_name_label),
                    error = state.nameError,
                )
                Spacer(Modifier.height(20.dp))
                OsirisDropdownField(
                    label = stringResource(R.string.account_type_label),
                    selected = state.type,
                    options = AccountType.entries,
                    optionLabel = { typeLabels.getValue(it) },
                    onSelect = viewModel::onTypeChange,
                )
                Spacer(Modifier.height(20.dp))
                if (state.isEdit) {
                    Text(
                        text = stringResource(R.string.account_balance_locked),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                } else {
                    OsirisTextField(
                        value = state.initialBalance,
                        onValueChange = viewModel::onInitialBalanceChange,
                        label = stringResource(R.string.account_initial_balance_label),
                        error = state.initialBalanceError,
                        keyboardType = KeyboardType.Decimal,
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
                        Text(stringResource(R.string.account_save))
                    }
                }
            }
        }
    }
}
