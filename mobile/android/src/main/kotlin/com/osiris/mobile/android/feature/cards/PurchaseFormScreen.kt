package com.osiris.mobile.android.feature.cards

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
import androidx.compose.material3.Card
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
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.PurchasePreview
import com.osiris.mobile.presentation.cards.PurchaseFormEvent
import com.osiris.mobile.presentation.cards.PurchaseFormViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PurchaseFormScreen(
    cardId: String,
    onDone: () -> Unit,
    viewModel: PurchaseFormViewModel = koinViewModel { parametersOf(cardId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                PurchaseFormEvent.NavigateBack -> onDone()
                is PurchaseFormEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    val selectedCategory = state.categories.firstOrNull { it.id == state.categoryId }
    val categoryOptions = remember(state.categories) { listOf<Category?>(null) + state.categories }
    val selectCategoryLabel = stringResource(R.string.purchase_category_label)

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.purchase_new)) },
                navigationIcon = {
                    IconButton(onClick = onDone) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
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
                label = stringResource(R.string.purchase_amount_label),
                error = state.amountError,
                keyboardType = KeyboardType.Decimal,
            )
            Spacer(Modifier.height(20.dp))
            OsirisDateField(
                label = stringResource(R.string.purchase_date_label),
                value = state.purchaseDate,
                onValueChange = viewModel::onPurchaseDateChange,
            )
            Spacer(Modifier.height(20.dp))
            OsirisTextField(
                value = state.description,
                onValueChange = viewModel::onDescriptionChange,
                label = stringResource(R.string.purchase_description_label),
                error = state.descriptionError,
            )
            Spacer(Modifier.height(20.dp))
            OsirisDropdownField(
                label = stringResource(R.string.purchase_category_label),
                selected = selectedCategory,
                options = categoryOptions,
                optionLabel = { it?.name ?: selectCategoryLabel },
                onSelect = { viewModel.onCategoryChange(it?.id) },
            )
            if (state.categoryError != null) {
                Spacer(Modifier.height(4.dp))
                Text(state.categoryError!!, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
            }
            Spacer(Modifier.height(20.dp))
            OsirisTextField(
                value = state.installments,
                onValueChange = viewModel::onInstallmentsChange,
                label = stringResource(R.string.purchase_installments_label),
                error = state.installmentsError,
                keyboardType = KeyboardType.Number,
            )
            Spacer(Modifier.height(20.dp))
            OsirisTextField(
                value = state.notes,
                onValueChange = viewModel::onNotesChange,
                label = stringResource(R.string.purchase_notes_label),
                error = state.notesError,
            )
            Spacer(Modifier.height(20.dp))
            if (state.isPreviewLoading) {
                CircularProgressIndicator(modifier = Modifier.size(24.dp), color = MaterialTheme.colorScheme.primary)
            } else {
                state.preview?.let { PurchasePreviewCard(it) }
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
                    Text(stringResource(R.string.purchase_save))
                }
            }
        }
    }
}

@Composable
private fun PurchasePreviewCard(preview: PurchasePreview) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Text(stringResource(R.string.purchase_preview), style = MaterialTheme.typography.titleSmall)
            Spacer(Modifier.height(8.dp))
            preview.installments.forEach { installment ->
                Text(
                    text = "${installment.number}/${preview.installments.size} - ${Money.brl(installment.amount)} - ${statementReference(installment.referenceMonth, installment.referenceYear)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Spacer(Modifier.height(8.dp))
            Text(
                text = "${stringResource(R.string.card_used_limit)}: ${Money.brl(preview.projectedUsedLimit)}",
                style = MaterialTheme.typography.bodyMedium,
            )
        }
    }
}
