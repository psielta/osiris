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
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
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
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CsvAmountMode
import com.osiris.mobile.presentation.accounts.CsvColumn
import com.osiris.mobile.presentation.accounts.CsvColumnOption
import com.osiris.mobile.presentation.accounts.CsvImportEvent
import com.osiris.mobile.presentation.accounts.CsvImportRow
import com.osiris.mobile.presentation.accounts.CsvImportUiState
import com.osiris.mobile.presentation.accounts.CsvImportViewModel
import org.koin.androidx.compose.koinViewModel
import org.koin.core.parameter.parametersOf
import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val inflowColor = Color(0xFF16A34A)
private val displayDateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy")

private val delimiterOptions = listOf(";" to ";", "," to ",", "\t" to "Tab")
private val encodingOptions = listOf("utf-8" to "UTF-8", "windows-1252" to "Windows-1252")
private val dateFormatOptions = listOf("dd/MM/yyyy", "yyyy-MM-dd", "dd/MM/yy", "dd-MM-yyyy", "MM/dd/yyyy")
private val decimalSeparatorOptions = listOf("," to ",", "." to ".")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CsvImportScreen(
    accountId: String,
    onDone: () -> Unit,
    viewModel: CsvImportViewModel = koinViewModel { parametersOf(accountId) },
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val context = LocalContext.current

    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        val file = uri?.let { readCsvFile(context, it) }
        if (file != null) {
            viewModel.onFileSelected(file.first, file.second)
        }
    }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is CsvImportEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
                is CsvImportEvent.Done -> {
                    Toast.makeText(context, event.message, Toast.LENGTH_LONG).show()
                    onDone()
                }
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.import_csv_title)) },
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
            state.isAnalyzing -> Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }

            !state.hasAnalysis -> FilePicker(
                onPick = { picker.launch(arrayOf("*/*")) },
                modifier = Modifier.fillMaxSize().padding(padding),
            )

            !state.hasPreview -> MappingContent(
                state = state,
                viewModel = viewModel,
                onChangeFile = { picker.launch(arrayOf("*/*")) },
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
            text = stringResource(R.string.import_csv_help),
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center,
        )
        Spacer(Modifier.height(20.dp))
        Button(onClick = onPick) { Text(stringResource(R.string.import_csv_select_file)) }
    }
}

@Composable
private fun MappingContent(
    state: CsvImportUiState,
    viewModel: CsvImportViewModel,
    onChangeFile: () -> Unit,
    modifier: Modifier,
) {
    val mapping = state.mapping
    val columns = state.columns
    val noneLabel = stringResource(R.string.csv_col_none)
    val columnOptions = remember(columns) { listOf<CsvColumnOption?>(null) + columns }

    Column(modifier.verticalScroll(rememberScrollState()).padding(16.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                text = state.fileName.orEmpty(),
                style = MaterialTheme.typography.bodyMedium,
                modifier = Modifier.weight(1f),
            )
            TextButton(onClick = onChangeFile) { Text(stringResource(R.string.csv_change_file)) }
        }

        Spacer(Modifier.height(8.dp))
        SectionTitle(stringResource(R.string.csv_format_section))
        OsirisDropdownField(
            label = stringResource(R.string.csv_delimiter),
            selected = mapping.delimiter,
            options = delimiterOptions.map { it.first },
            optionLabel = { value -> delimiterOptions.first { it.first == value }.second },
            onSelect = viewModel::setDelimiter,
        )
        Spacer(Modifier.height(8.dp))
        OsirisDropdownField(
            label = stringResource(R.string.csv_encoding),
            selected = mapping.encoding,
            options = encodingOptions.map { it.first },
            optionLabel = { value -> encodingOptions.firstOrNull { it.first == value }?.second ?: value },
            onSelect = viewModel::setEncoding,
        )
        Spacer(Modifier.height(8.dp))
        OsirisDropdownField(
            label = stringResource(R.string.csv_header_line),
            selected = mapping.headerLineIndex,
            options = state.sampleRows.indices.toList(),
            optionLabel = { index -> headerLineLabel(index, state.sampleRows.getOrNull(index), mapping.delimiter) },
            onSelect = viewModel::setHeaderLineIndex,
        )
        Spacer(Modifier.height(8.dp))
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(stringResource(R.string.csv_has_header), modifier = Modifier.weight(1f))
            Switch(checked = mapping.hasHeader, onCheckedChange = viewModel::setHasHeader)
        }

        Spacer(Modifier.height(8.dp))
        SamplePreview(state.sampleRows, mapping.headerLineIndex, mapping.delimiter)

        Spacer(Modifier.height(16.dp))
        SectionTitle(stringResource(R.string.csv_columns_section))
        ColumnDropdown(
            label = stringResource(R.string.csv_col_date),
            selectedIndex = mapping.dateColumn,
            options = columnOptions,
            noneLabel = noneLabel,
            onSelect = { viewModel.setColumn(CsvColumn.Date, it) },
        )
        Spacer(Modifier.height(8.dp))
        ColumnDropdown(
            label = stringResource(R.string.csv_col_description),
            selectedIndex = mapping.descriptionColumn,
            options = columnOptions,
            noneLabel = noneLabel,
            onSelect = { viewModel.setColumn(CsvColumn.Description, it) },
        )
        Spacer(Modifier.height(8.dp))
        ColumnDropdown(
            label = stringResource(R.string.csv_col_description2),
            selectedIndex = mapping.secondaryDescriptionColumn,
            options = columnOptions,
            noneLabel = noneLabel,
            onSelect = { viewModel.setColumn(CsvColumn.SecondaryDescription, it) },
        )

        Spacer(Modifier.height(12.dp))
        val signedLabel = stringResource(R.string.csv_mode_signed)
        val debitCreditLabel = stringResource(R.string.csv_mode_debit_credit)
        val typeLabel = stringResource(R.string.csv_mode_type)
        OsirisDropdownField(
            label = stringResource(R.string.csv_amount_mode),
            selected = mapping.amountMode,
            options = listOf(CsvAmountMode.SignedAmount, CsvAmountMode.DebitCredit, CsvAmountMode.TypeColumn),
            optionLabel = { mode ->
                when (mode) {
                    CsvAmountMode.SignedAmount -> signedLabel
                    CsvAmountMode.DebitCredit -> debitCreditLabel
                    CsvAmountMode.TypeColumn -> typeLabel
                }
            },
            onSelect = viewModel::setAmountMode,
        )
        Spacer(Modifier.height(8.dp))
        when (mapping.amountMode) {
            CsvAmountMode.SignedAmount -> ColumnDropdown(
                label = stringResource(R.string.csv_col_amount),
                selectedIndex = mapping.amountColumn,
                options = columnOptions,
                noneLabel = noneLabel,
                onSelect = { viewModel.setColumn(CsvColumn.Amount, it) },
            )

            CsvAmountMode.DebitCredit -> {
                ColumnDropdown(
                    label = stringResource(R.string.csv_col_credit),
                    selectedIndex = mapping.creditColumn,
                    options = columnOptions,
                    noneLabel = noneLabel,
                    onSelect = { viewModel.setColumn(CsvColumn.Credit, it) },
                )
                Spacer(Modifier.height(8.dp))
                ColumnDropdown(
                    label = stringResource(R.string.csv_col_debit),
                    selectedIndex = mapping.debitColumn,
                    options = columnOptions,
                    noneLabel = noneLabel,
                    onSelect = { viewModel.setColumn(CsvColumn.Debit, it) },
                )
            }

            CsvAmountMode.TypeColumn -> {
                ColumnDropdown(
                    label = stringResource(R.string.csv_col_amount),
                    selectedIndex = mapping.amountColumn,
                    options = columnOptions,
                    noneLabel = noneLabel,
                    onSelect = { viewModel.setColumn(CsvColumn.Amount, it) },
                )
                Spacer(Modifier.height(8.dp))
                ColumnDropdown(
                    label = stringResource(R.string.csv_col_type),
                    selectedIndex = mapping.typeColumn,
                    options = columnOptions,
                    noneLabel = noneLabel,
                    onSelect = { viewModel.setColumn(CsvColumn.Type, it) },
                )
            }
        }
        Spacer(Modifier.height(8.dp))
        ColumnDropdown(
            label = stringResource(R.string.csv_col_external_id),
            selectedIndex = mapping.externalIdColumn,
            options = columnOptions,
            noneLabel = noneLabel,
            onSelect = { viewModel.setColumn(CsvColumn.ExternalId, it) },
        )

        Spacer(Modifier.height(16.dp))
        SectionTitle(stringResource(R.string.csv_formats_section))
        OsirisDropdownField(
            label = stringResource(R.string.csv_date_format),
            selected = mapping.dateFormat,
            options = dateFormatOptions,
            optionLabel = { it },
            onSelect = viewModel::setDateFormat,
        )
        Spacer(Modifier.height(8.dp))
        OsirisDropdownField(
            label = stringResource(R.string.csv_decimal_separator),
            selected = mapping.decimalSeparator,
            options = decimalSeparatorOptions.map { it.first },
            optionLabel = { value -> decimalSeparatorOptions.first { it.first == value }.second },
            onSelect = viewModel::setDecimalSeparator,
        )

        Spacer(Modifier.height(20.dp))
        Button(
            onClick = viewModel::preview,
            enabled = !state.isPreviewing,
            modifier = Modifier.fillMaxWidth(),
        ) {
            if (state.isPreviewing) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    strokeWidth = 2.dp,
                    color = MaterialTheme.colorScheme.onPrimary,
                )
            } else {
                Text(stringResource(R.string.csv_preview))
            }
        }
    }
}

@Composable
private fun SectionTitle(text: String) {
    Text(
        text = text,
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(bottom = 8.dp),
    )
}

@Composable
private fun ColumnDropdown(
    label: String,
    selectedIndex: Int?,
    options: List<CsvColumnOption?>,
    noneLabel: String,
    onSelect: (Int?) -> Unit,
) {
    OsirisDropdownField(
        label = label,
        selected = options.firstOrNull { it?.index == selectedIndex },
        options = options,
        optionLabel = { it?.label ?: noneLabel },
        onSelect = { onSelect(it?.index) },
    )
}

@Composable
private fun SamplePreview(sampleRows: List<List<String>>, headerLineIndex: Int, delimiter: String) {
    val start = headerLineIndex.coerceIn(0, maxOf(0, sampleRows.size - 1))
    val preview = sampleRows.drop(start).take(4)
    if (preview.isEmpty()) return
    Column {
        preview.forEach { row ->
            Text(
                text = row.joinToString(displayDelimiter(delimiter)),
                style = MaterialTheme.typography.bodySmall,
                fontFamily = FontFamily.Monospace,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun PreviewContent(
    state: CsvImportUiState,
    viewModel: CsvImportViewModel,
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
                CsvRow(
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
private fun CsvRow(
    row: CsvImportRow,
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

private fun headerLineLabel(index: Int, row: List<String>?, delimiter: String): String {
    val joined = row?.joinToString(displayDelimiter(delimiter))?.takeIf { it.isNotBlank() } ?: ""
    val preview = if (joined.length > 40) joined.take(40) + "…" else joined
    return "${index + 1}: $preview"
}

private fun displayDelimiter(delimiter: String): String = if (delimiter == "\t") " | " else "$delimiter "

private fun formatDate(iso: String): String =
    runCatching { LocalDate.parse(iso).format(displayDateFormat) }.getOrDefault(iso)

private fun readCsvFile(context: Context, uri: Uri): Pair<String, ByteArray>? {
    val bytes = context.contentResolver.openInputStream(uri)?.use { it.readBytes() } ?: return null
    val name = queryDisplayName(context, uri) ?: "extrato.csv"
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
