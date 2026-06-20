package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CsvAmountMode
import com.osiris.mobile.domain.model.CsvAnalysis
import com.osiris.mobile.domain.model.CsvImportMapping
import com.osiris.mobile.domain.model.OfxImportLine
import com.osiris.mobile.domain.model.OfxImportResult
import com.osiris.mobile.domain.model.OfxImportSelection
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.repository.CategoryRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/** One selectable column: its positional index plus the label shown in the dropdown. */
data class CsvColumnOption(val index: Int, val label: String)

data class CsvImportRow(
    val line: OfxImportLine,
    val include: Boolean,
    val categoryId: String?,
)

data class CsvImportUiState(
    val fileName: String? = null,
    val isAnalyzing: Boolean = false,
    val isPreviewing: Boolean = false,
    val isConfirming: Boolean = false,
    val hasAnalysis: Boolean = false,
    val hasPreview: Boolean = false,
    val sampleRows: List<List<String>> = emptyList(),
    val mapping: CsvImportMapping = CsvImportMapping(),
    val columns: List<CsvColumnOption> = emptyList(),
    val newCount: Int = 0,
    val duplicateCount: Int = 0,
    val rows: List<CsvImportRow> = emptyList(),
    val categories: List<Category> = emptyList(),
) {
    val selectedCount: Int get() = rows.count { it.include }
}

sealed interface CsvImportEvent {
    data class ShowMessage(val message: String) : CsvImportEvent
    data class Done(val message: String) : CsvImportEvent
}

class CsvImportViewModel(
    private val accountRepository: AccountRepository,
    private val categoryRepository: CategoryRepository,
    private val accountId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(CsvImportUiState())
    val state: StateFlow<CsvImportUiState> = _state.asStateFlow()

    private val _events = Channel<CsvImportEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    private var fileBytes: ByteArray? = null

    fun onFileSelected(fileName: String, bytes: ByteArray) {
        fileBytes = bytes
        _state.update {
            it.copy(
                fileName = fileName,
                isAnalyzing = true,
                hasAnalysis = false,
                hasPreview = false,
                rows = emptyList(),
            )
        }
        analyze(keepMapping = false)
    }

    /**
     * Re-runs analysis on the already-selected file. When [keepMapping] is true the user's column and
     * format choices are preserved, and their chosen delimiter/encoding are sent as query params so the
     * backend re-splits the sample with them; this is used after a delimiter/encoding change. On the first
     * call ([keepMapping] false) both params are omitted so the backend auto-detects them.
     */
    private fun analyze(keepMapping: Boolean) {
        val bytes = fileBytes ?: return
        val name = _state.value.fileName ?: return
        val delimiter = if (keepMapping) _state.value.mapping.delimiter else null
        val encoding = if (keepMapping) _state.value.mapping.encoding else null
        viewModelScope.launch {
            when (val result = accountRepository.analyzeCsvImport(accountId, name, bytes, delimiter, encoding)) {
                is OsirisResult.Success -> {
                    val analysis = result.value
                    val mapping = if (keepMapping) {
                        _state.value.mapping.copy(
                            delimiter = analysis.delimiter,
                            encoding = analysis.encoding,
                        )
                    } else {
                        analysis.savedMapping ?: guessMapping(analysis.sampleRows, analysis)
                    }
                    _state.update {
                        it.copy(
                            isAnalyzing = false,
                            hasAnalysis = true,
                            sampleRows = analysis.sampleRows,
                            mapping = mapping,
                            columns = columnsFor(analysis.sampleRows, mapping),
                        )
                    }
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isAnalyzing = false) }
                    _events.send(CsvImportEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    fun setDelimiter(delimiter: String) {
        _state.update { it.copy(mapping = it.mapping.copy(delimiter = delimiter), isAnalyzing = true) }
        analyze(keepMapping = true)
    }

    fun setEncoding(encoding: String) {
        _state.update { it.copy(mapping = it.mapping.copy(encoding = encoding), isAnalyzing = true) }
        analyze(keepMapping = true)
    }

    fun setHeaderLineIndex(index: Int) = updateMapping { it.copy(headerLineIndex = index) }

    fun setHasHeader(hasHeader: Boolean) = updateMapping { it.copy(hasHeader = hasHeader) }

    fun setAmountMode(mode: CsvAmountMode) = _state.update { it.copy(mapping = it.mapping.copy(amountMode = mode)) }

    fun setDateFormat(format: String) = _state.update { it.copy(mapping = it.mapping.copy(dateFormat = format)) }

    fun setDecimalSeparator(separator: String) =
        _state.update { it.copy(mapping = it.mapping.copy(decimalSeparator = separator)) }

    fun setColumn(field: CsvColumn, index: Int?) = _state.update { state ->
        val mapping = state.mapping
        val updated = when (field) {
            CsvColumn.Date -> mapping.copy(dateColumn = index ?: 0)
            CsvColumn.Description -> mapping.copy(descriptionColumn = index ?: 0)
            CsvColumn.SecondaryDescription -> mapping.copy(secondaryDescriptionColumn = index)
            CsvColumn.Amount -> mapping.copy(amountColumn = index)
            CsvColumn.Credit -> mapping.copy(creditColumn = index)
            CsvColumn.Debit -> mapping.copy(debitColumn = index)
            CsvColumn.Type -> mapping.copy(typeColumn = index)
            CsvColumn.ExternalId -> mapping.copy(externalIdColumn = index)
        }
        state.copy(mapping = updated)
    }

    /** Recomputes the available columns after a header-line/has-header change that alters cell labels. */
    private fun updateMapping(transform: (CsvImportMapping) -> CsvImportMapping) = _state.update { state ->
        val mapping = transform(state.mapping)
        state.copy(mapping = mapping, columns = columnsFor(state.sampleRows, mapping))
    }

    fun preview() {
        val bytes = fileBytes ?: return
        val name = _state.value.fileName ?: return
        _state.update { it.copy(isPreviewing = true) }
        viewModelScope.launch {
            when (val result = accountRepository.previewCsvImport(accountId, name, bytes, _state.value.mapping)) {
                is OsirisResult.Success -> {
                    val preview = result.value
                    _state.update {
                        it.copy(
                            isPreviewing = false,
                            hasPreview = true,
                            newCount = preview.newCount,
                            duplicateCount = preview.duplicateCount,
                            rows = preview.lines.map { line ->
                                CsvImportRow(line = line, include = !line.isDuplicate, categoryId = null)
                            },
                        )
                    }
                    loadCategories()
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isPreviewing = false) }
                    _events.send(CsvImportEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    private fun loadCategories() {
        viewModelScope.launch {
            val result = categoryRepository.list()
            if (result is OsirisResult.Success) {
                _state.update { it.copy(categories = result.value.filter { category -> category.isActive }) }
            }
        }
    }

    fun toggleInclude(rowKey: String) = _state.update { state ->
        state.copy(rows = state.rows.map { row ->
            if (row.line.rowKey == rowKey) row.copy(include = !row.include) else row
        })
    }

    fun setCategory(rowKey: String, categoryId: String?) = _state.update { state ->
        state.copy(rows = state.rows.map { row ->
            if (row.line.rowKey == rowKey) row.copy(categoryId = categoryId) else row
        })
    }

    fun applyCategoryToAll(categoryId: String?) = _state.update { state ->
        state.copy(rows = state.rows.map { it.copy(categoryId = categoryId) })
    }

    fun confirm() {
        val selected = _state.value.rows.filter { it.include }
        if (selected.isEmpty()) {
            viewModelScope.launch {
                _events.send(CsvImportEvent.ShowMessage("Selecione ao menos um lançamento para importar."))
            }
            return
        }

        _state.update { it.copy(isConfirming = true) }
        viewModelScope.launch {
            val selections = selected.map { row ->
                OfxImportSelection(
                    externalId = row.line.externalId,
                    occurredOn = row.line.occurredOn,
                    amount = row.line.amount,
                    type = row.line.type,
                    description = row.line.description,
                    categoryId = row.categoryId,
                )
            }

            when (val result = accountRepository.confirmOfxImport(accountId, selections)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isConfirming = false) }
                    _events.send(CsvImportEvent.Done(summaryOf(result.value)))
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isConfirming = false) }
                    _events.send(CsvImportEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    private fun summaryOf(result: OfxImportResult): String =
        if (result.skippedDuplicates > 0) {
            "${result.imported} lançamento(s) importado(s), ${result.skippedDuplicates} ignorado(s)."
        } else {
            "${result.imported} lançamento(s) importado(s)."
        }
}

enum class CsvColumn { Date, Description, SecondaryDescription, Amount, Credit, Debit, Type, ExternalId }

/**
 * The header cells the user can map columns to. When [CsvImportMapping.hasHeader] is set we label them
 * with the cells of the header row; otherwise we fall back to positional "Coluna N" labels sized to the
 * widest sample row so every column remains selectable.
 */
private fun columnsFor(sampleRows: List<List<String>>, mapping: CsvImportMapping): List<CsvColumnOption> {
    if (mapping.hasHeader) {
        val header = sampleRows.getOrNull(mapping.headerLineIndex)
            ?: sampleRows.firstOrNull { it.isNotEmpty() }
            ?: return emptyList()
        return header.mapIndexed { index, cell ->
            CsvColumnOption(index, cell.trim().ifBlank { "Coluna ${index + 1}" })
        }
    }
    val width = sampleRows.maxOfOrNull { it.size } ?: 0
    return (0 until width).map { CsvColumnOption(it, "Coluna ${it + 1}") }
}

/**
 * Picks reasonable default columns when the backend has no saved mapping: the suggested header line plus
 * simple keyword heuristics over the header cells (falls back to the first columns when nothing matches).
 */
private fun guessMapping(
    sampleRows: List<List<String>>,
    analysis: CsvAnalysis,
): CsvImportMapping {
    val base = CsvImportMapping(
        delimiter = analysis.delimiter,
        encoding = analysis.encoding,
        hasHeader = true,
        headerLineIndex = analysis.suggestedHeaderLineIndex,
    )
    val header = sampleRows.getOrNull(analysis.suggestedHeaderLineIndex)?.map { it.trim().lowercase() }
        ?: return base
    fun find(vararg tokens: String): Int? =
        header.indexOfFirst { cell -> tokens.any { cell.contains(it) } }.takeIf { it >= 0 }

    val dateColumn = find("data", "date") ?: 0
    val amountColumn = find("valor", "amount", "montante")
    val descriptionColumn = find("histórico", "historico", "descrição", "descricao", "lançamento", "lancamento", "memo")
        ?: header.indices.firstOrNull { it != dateColumn && it != amountColumn }
        ?: 0
    return base.copy(
        dateColumn = dateColumn,
        descriptionColumn = descriptionColumn,
        amountColumn = amountColumn,
    )
}
