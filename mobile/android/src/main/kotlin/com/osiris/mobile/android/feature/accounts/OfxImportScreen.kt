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
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.presentation.accounts.OfxImportEvent
import com.osiris.mobile.presentation.accounts.OfxImportRow
import com.osiris.mobile.presentation.accounts.OfxImportUiState
import com.osiris.mobile.presentation.accounts.OfxImportViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf
import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val inflowColor = Color(0xFF16A34A)
private val dateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OfxImportScreen(
    accountId: String,
    onDone: () -> Unit,
    viewModel: OfxImportViewModel = koinViewModel { parametersOf(accountId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val context = LocalContext.current

    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        val file = uri?.let { readOfxFile(context, it) }
        if (file != null) {
            viewModel.onFileSelected(file.first, file.second)
        }
    }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is OfxImportEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
                is OfxImportEvent.Done -> {
                    Toast.makeText(context, event.message, Toast.LENGTH_LONG).show()
                    onDone()
                }
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.import_ofx_title)) },
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
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }

            !state.hasPreview -> FilePicker(
                onPick = { picker.launch(arrayOf("*/*")) },
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
            text = stringResource(R.string.import_ofx_help),
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center,
        )
        Spacer(Modifier.height(20.dp))
        Button(onClick = onPick) { Text(stringResource(R.string.import_ofx_select_file)) }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun PreviewContent(
    state: OfxImportUiState,
    viewModel: OfxImportViewModel,
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
                OfxRow(
                    row = row,
                    categories = state.categories,
                    noCategoryLabel = noCategoryLabel,
                    onToggle = { viewModel.toggleInclude(row.line.rowKey) },
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

@Composable
private fun OfxRow(
    row: OfxImportRow,
    categories: List<Category>,
    noCategoryLabel: String,
    onToggle: () -> Unit,
    onCategory: (String?) -> Unit,
) {
    val categoryOptions = remember(categories) { listOf<Category?>(null) + categories }
    val selectedCategory = categories.find { it.id == row.categoryId }
    val sign = if (row.line.isInflow) "+" else "−"
    val dateLabel = if (row.line.isDuplicate) {
        formatDate(row.line.occurredOn) + " · " + stringResource(R.string.import_ofx_already_imported)
    } else {
        formatDate(row.line.occurredOn)
    }

    Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = row.include, onCheckedChange = { onToggle() })
            Spacer(Modifier.size(8.dp))
            Column(Modifier.weight(1f)) {
                Text(row.line.description, style = MaterialTheme.typography.bodyLarge)
                Text(
                    text = dateLabel,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Text(
                text = "$sign${Money.brl(row.line.amount)}",
                style = MaterialTheme.typography.bodyLarge,
                color = if (row.line.isInflow) inflowColor else MaterialTheme.colorScheme.error,
            )
        }
        if (row.include) {
            Spacer(Modifier.height(8.dp))
            OsirisDropdownField(
                label = stringResource(R.string.movement_category_label),
                selected = selectedCategory,
                options = categoryOptions,
                optionLabel = { it?.name ?: noCategoryLabel },
                onSelect = { onCategory(it?.id) },
            )
        }
    }
}

private fun formatDate(iso: String): String =
    runCatching { LocalDate.parse(iso).format(dateFormat) }.getOrDefault(iso)

private fun readOfxFile(context: Context, uri: Uri): Pair<String, ByteArray>? {
    val bytes = context.contentResolver.openInputStream(uri)?.use { it.readBytes() } ?: return null
    val name = queryDisplayName(context, uri) ?: "extrato.ofx"
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
