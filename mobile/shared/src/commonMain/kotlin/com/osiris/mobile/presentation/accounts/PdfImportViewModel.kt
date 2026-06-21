package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Category
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

data class PdfImportRow(
    val line: OfxImportLine,
    val include: Boolean,
    val categoryId: String?,
)

data class PdfImportUiState(
    val isUploading: Boolean = false,
    val isConfirming: Boolean = false,
    val hasPreview: Boolean = false,
    val periodStart: String? = null,
    val periodEnd: String? = null,
    val totalCount: Int = 0,
    val newCount: Int = 0,
    val duplicateCount: Int = 0,
    val rows: List<PdfImportRow> = emptyList(),
    val categories: List<Category> = emptyList(),
) {
    val selectedCount: Int get() = rows.count { it.include }
}

sealed interface PdfImportEvent {
    data class ShowMessage(val message: String) : PdfImportEvent
    data class Done(val message: String) : PdfImportEvent
}

class PdfImportViewModel(
    private val accountRepository: AccountRepository,
    private val categoryRepository: CategoryRepository,
    private val accountId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(PdfImportUiState())
    val state: StateFlow<PdfImportUiState> = _state.asStateFlow()

    private val _events = Channel<PdfImportEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        loadCategories()
    }

    private fun loadCategories() {
        viewModelScope.launch {
            val result = categoryRepository.list()
            if (result is OsirisResult.Success) {
                _state.update { it.copy(categories = result.value.filter { category -> category.isActive }) }
            }
        }
    }

    fun onFileSelected(fileName: String, bytes: ByteArray) {
        _state.update { it.copy(isUploading = true) }
        viewModelScope.launch {
            when (val result = accountRepository.previewPdfImport(accountId, fileName, bytes)) {
                is OsirisResult.Success -> {
                    val preview = result.value
                    _state.update {
                        it.copy(
                            isUploading = false,
                            hasPreview = true,
                            periodStart = preview.periodStart,
                            periodEnd = preview.periodEnd,
                            totalCount = preview.totalCount,
                            newCount = preview.newCount,
                            duplicateCount = preview.duplicateCount,
                            rows = preview.lines.map { line ->
                                PdfImportRow(line = line, include = !line.isDuplicate, categoryId = null)
                            },
                        )
                    }
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isUploading = false) }
                    _events.send(PdfImportEvent.ShowMessage(result.error.message))
                }
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
                _events.send(PdfImportEvent.ShowMessage("Selecione ao menos um lançamento para importar."))
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
                    _events.send(PdfImportEvent.Done(summaryOf(result.value)))
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isConfirming = false) }
                    _events.send(PdfImportEvent.ShowMessage(result.error.message))
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
