package com.osiris.mobile.android.feature.accounts

import android.content.Context
import android.net.Uri
import android.provider.OpenableColumns
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
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
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.presentation.accounts.PdfImportEvent
import com.osiris.mobile.presentation.accounts.PdfImportUiState
import com.osiris.mobile.presentation.accounts.PdfImportViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PdfImportScreen(
    accountId: String,
    onDone: () -> Unit,
    viewModel: PdfImportViewModel = koinViewModel { parametersOf(accountId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val context = LocalContext.current

    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        val file = uri?.let { readPdfFile(context, it) }
        if (file != null) {
            viewModel.onFileSelected(file.first, file.second)
        }
    }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is PdfImportEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
                is PdfImportEvent.Done -> {
                    Toast.makeText(context, event.message, Toast.LENGTH_LONG).show()
                    onDone()
                }
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.import_pdf_title)) },
                navigationIcon = {
                    IconButton(onClick = onDone) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        when {
            state.isUploading -> Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
                    Spacer(Modifier.height(16.dp))
                    Text(
                        text = stringResource(R.string.import_pdf_processing),
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }

            !state.hasPreview -> FilePicker(
                onPick = { picker.launch(arrayOf("application/pdf")) },
                modifier = Modifier.fillMaxSize().padding(padding),
            )

            else -> PreviewContent(state, viewModel, Modifier.fillMaxSize().padding(padding))
        }
    }
}

@Composable
private fun FilePicker(onPick: () -> Unit, modifier: Modifier) {
    Column(
        modifier = modifier.padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(
            text = stringResource(R.string.import_pdf_help),
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center,
        )
        Spacer(Modifier.height(20.dp))
        Button(onClick = onPick) { Text(stringResource(R.string.import_pdf_select_file)) }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun PreviewContent(
    state: PdfImportUiState,
    viewModel: PdfImportViewModel,
    modifier: Modifier,
) {
    val noCategoryLabel = stringResource(R.string.movement_no_category)
    val categoryOptions = remember(state.categories) { listOf<Category?>(null) + state.categories }

    Column(modifier) {
        Column(Modifier.padding(16.dp)) {
            Text(
                text = stringResource(R.string.import_ofx_summary, state.newCount, state.duplicateCount),
                style = MaterialTheme.typography.bodyMedium,
            )
            if (state.suggestedCount > 0) {
                Text(
                    text = stringResource(R.string.import_reconcile_count, state.suggestedCount),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary,
                )
            }
            Spacer(Modifier.height(12.dp))
            OsirisDropdownField(
                label = stringResource(R.string.import_ofx_category_all),
                selected = null as Category?,
                options = categoryOptions,
                optionLabel = { it?.name ?: noCategoryLabel },
                onSelect = { viewModel.applyCategoryToAll(it?.id) },
            )
        }
        HorizontalDivider()
        LazyColumn(modifier = Modifier.weight(1f), contentPadding = PaddingValues(vertical = 8.dp)) {
            items(state.rows, key = { it.line.rowKey }) { row ->
                ImportPreviewLineRow(
                    line = row.line,
                    action = row.action,
                    reconcileWithMovementId = row.reconcileWithMovementId,
                    categoryId = row.categoryId,
                    categories = state.categories,
                    noCategoryLabel = noCategoryLabel,
                    onAction = { viewModel.setAction(row.line.rowKey, it) },
                    onReconcileTarget = { viewModel.setReconcileTarget(row.line.rowKey, it) },
                    onCategory = { viewModel.setCategory(row.line.rowKey, it) },
                )
                HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            }
        }
        Button(
            onClick = viewModel::confirm,
            enabled = !state.isConfirming && state.selectedCount > 0,
            modifier = Modifier.fillMaxWidth().padding(16.dp),
        ) {
            if (state.isConfirming) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    strokeWidth = 2.dp,
                    color = MaterialTheme.colorScheme.onPrimary,
                )
            } else {
                Text(stringResource(R.string.import_ofx_confirm, state.selectedCount))
            }
        }
    }
}

private fun readPdfFile(context: Context, uri: Uri): Pair<String, ByteArray>? {
    val bytes = context.contentResolver.openInputStream(uri)?.use { it.readBytes() } ?: return null
    val name = queryDisplayName(context, uri) ?: "extrato.pdf"
    return name to bytes
}

private fun queryDisplayName(context: Context, uri: Uri): String? {
    context.contentResolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)?.use { cursor ->
        if (cursor.moveToFirst()) {
            val index = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME)
            if (index >= 0) {
                return cursor.getString(index)
            }
        }
    }
    return null
}
