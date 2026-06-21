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

data class OfxImportRow(
    val line: OfxImportLine,
    val action: ImportLineAction,
    val reconcileWithMovementId: String?,
    val categoryId: String?,
) {
    val include: Boolean get() = action != ImportLineAction.Ignore
}

data class OfxImportUiState(
    val isUploading: Boolean = false,
    val isConfirming: Boolean = false,
    val hasPreview: Boolean = false,
    val periodStart: String? = null,
    val periodEnd: String? = null,
    val totalCount: Int = 0,
    val newCount: Int = 0,
    val duplicateCount: Int = 0,
    val suggestedCount: Int = 0,
    val rows: List<OfxImportRow> = emptyList(),
    val categories: List<Category> = emptyList(),
) {
    val selectedCount: Int get() = rows.count { it.include }
}

sealed interface OfxImportEvent {
    data class ShowMessage(val message: String) : OfxImportEvent
    data class Done(val message: String) : OfxImportEvent
}

class OfxImportViewModel(
    private val accountRepository: AccountRepository,
    private val categoryRepository: CategoryRepository,
    private val accountId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(OfxImportUiState())
    val state: StateFlow<OfxImportUiState> = _state.asStateFlow()

    private val _events = Channel<OfxImportEvent>(Channel.BUFFERED)
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
            when (val result = accountRepository.previewOfxImport(accountId, fileName, bytes)) {
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
                            suggestedCount = preview.suggestedReconciliationCount,
                            rows = preview.lines.map { line ->
                                OfxImportRow(
                                    line = line,
                                    action = initialImportAction(line),
                                    reconcileWithMovementId = line.suggestedMovementId,
                                    categoryId = null,
                                )
                            },
                        )
                    }
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isUploading = false) }
                    _events.send(OfxImportEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    fun setAction(rowKey: String, action: ImportLineAction) = _state.update { state ->
        state.copy(rows = state.rows.map { row ->
            if (row.line.rowKey == rowKey) row.copy(action = action) else row
        })
    }

    fun setReconcileTarget(rowKey: String, movementId: String?) = _state.update { state ->
        state.copy(rows = state.rows.map { row ->
            if (row.line.rowKey == rowKey) row.copy(reconcileWithMovementId = movementId) else row
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
                _events.send(OfxImportEvent.ShowMessage("Selecione ao menos um lançamento para importar."))
            }
            return
        }

        _state.update { it.copy(isConfirming = true) }
        viewModelScope.launch {
            val selections = selected.map { it.toSelection() }

            when (val result = accountRepository.confirmOfxImport(accountId, selections)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isConfirming = false) }
                    _events.send(OfxImportEvent.Done(summaryOf(result.value)))
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isConfirming = false) }
                    _events.send(OfxImportEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    private fun summaryOf(result: OfxImportResult): String = importSummaryOf(result)
}

/** A line the user kept (not ignored) mapped to a confirm selection, carrying its reconcile choice. */
internal fun OfxImportRow.toSelection(): OfxImportSelection {
    val reconcileId = if (action == ImportLineAction.Reconcile) {
        reconcileWithMovementId ?: line.candidates.firstOrNull()?.movementId
    } else {
        null
    }
    return OfxImportSelection(
        externalId = line.externalId,
        occurredOn = line.occurredOn,
        amount = line.amount,
        type = line.type,
        description = line.description,
        categoryId = if (reconcileId == null) categoryId else null,
        reconcileWithMovementId = reconcileId,
    )
}

/** Shared pt-BR summary for a confirmed import (new + reconciled + skipped). */
internal fun importSummaryOf(result: OfxImportResult): String = buildString {
    append("${result.imported} lançamento(s) importado(s)")
    if (result.reconciled > 0) append(", ${result.reconciled} conciliado(s)")
    if (result.skippedDuplicates > 0) append(", ${result.skippedDuplicates} ignorado(s)")
    append(".")
}
