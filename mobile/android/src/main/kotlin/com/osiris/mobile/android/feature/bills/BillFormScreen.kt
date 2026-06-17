package com.osiris.mobile.android.feature.bills

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
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.presentation.bills.BillFormEvent
import com.osiris.mobile.presentation.bills.BillFormViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BillFormScreen(
    billId: String?,
    onDone: () -> Unit,
    viewModel: BillFormViewModel = koinViewModel { parametersOf(billId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                BillFormEvent.NavigateBack -> onDone()
                is BillFormEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    val selectedCategory = state.categories.firstOrNull { it.id == state.categoryId }
    val selectedAccount = state.accounts.firstOrNull { it.id == state.paymentAccountId }
    val categoryOptions = remember(state.categories) { listOf<Category?>(null) + state.categories }
    val accountOptions = remember(state.accounts) { listOf<Account?>(null) + state.accounts }
    val categoryPlaceholder = stringResource(R.string.bill_category_label)
    val noAccountLabel = stringResource(R.string.payment_no_account)

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(if (state.isEdit) R.string.bill_edit else R.string.bill_new)) },
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
            androidx.compose.foundation.layout.Box(Modifier.fillMaxSize().padding(padding), contentAlignment = androidx.compose.ui.Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }
            return@Scaffold
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(24.dp)
                .verticalScroll(rememberScrollState()),
        ) {
            OsirisTextField(
                value = state.description,
                onValueChange = viewModel::onDescriptionChange,
                label = stringResource(R.string.bill_description_label),
                error = state.descriptionError,
            )
            Spacer(Modifier.height(20.dp))
            OsirisTextField(
                value = state.amount,
                onValueChange = viewModel::onAmountChange,
                label = stringResource(R.string.bill_amount_label),
                error = state.amountError,
                keyboardType = KeyboardType.Decimal,
            )
            Spacer(Modifier.height(20.dp))
            OsirisDateField(
                label = stringResource(R.string.bill_due_date_label),
                value = state.dueDate,
                onValueChange = viewModel::onDueDateChange,
            )
            if (state.dueDateError != null) {
                Spacer(Modifier.height(4.dp))
                Text(state.dueDateError!!, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
            }
            Spacer(Modifier.height(20.dp))
            OsirisDropdownField(
                label = stringResource(R.string.bill_category_label),
                selected = selectedCategory,
                options = categoryOptions,
                optionLabel = { it?.name ?: categoryPlaceholder },
                onSelect = { viewModel.onCategoryChange(it?.id) },
            )
            if (state.categoryError != null) {
                Spacer(Modifier.height(4.dp))
                Text(state.categoryError!!, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
            }
            Spacer(Modifier.height(20.dp))
            OsirisDropdownField(
                label = stringResource(R.string.bill_payment_account_label),
                selected = selectedAccount,
                options = accountOptions,
                optionLabel = { it?.name ?: noAccountLabel },
                onSelect = { viewModel.onPaymentAccountChange(it?.id) },
            )
            Spacer(Modifier.height(20.dp))
            OsirisTextField(
                value = state.notes,
                onValueChange = viewModel::onNotesChange,
                label = stringResource(R.string.bill_notes_label),
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
                    Text(stringResource(R.string.bill_save))
                }
            }
        }
    }
}
