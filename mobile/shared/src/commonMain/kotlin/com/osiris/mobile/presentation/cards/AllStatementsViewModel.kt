package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.CreditCardStatementOverview
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AllStatementsUiState(
    val statements: List<CreditCardStatementOverview> = emptyList(),
    val range: DateRangeFilterUiState = currentMonthRange(),
    val isLoading: Boolean = true,
    val error: String? = null,
    val filterError: String? = null,
)

class AllStatementsViewModel(
    private val cardRepository: CardRepository,
    private val dataChangeBus: DataChangeBus,
) : ViewModel() {

    private val _state = MutableStateFlow(AllStatementsUiState())
    val state: StateFlow<AllStatementsUiState> = _state.asStateFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Cards) { load() }
    }

    fun load() {
        val range = _state.value.range
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = cardRepository.listAllStatements(range.from, range.to)) {
                is OsirisResult.Success -> _state.update {
                    it.copy(statements = result.value, isLoading = false)
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }

    fun selectCurrentMonth() = selectRange(currentMonthRange())

    fun selectNextMonth() = selectRange(nextMonthRange())

    fun onCustomFromChange(value: String) {
        _state.update { it.copy(range = it.range.copy(customFrom = value), filterError = null) }
    }

    fun onCustomToChange(value: String) {
        _state.update { it.copy(range = it.range.copy(customTo = value), filterError = null) }
    }

    fun applyCustomRange() {
        val range = _state.value.range
        if (!isValidDateRange(range.customFrom, range.customTo)) {
            _state.update { it.copy(filterError = "Informe um periodo valido.") }
            return
        }

        selectRange(range.copy(from = range.customFrom, to = range.customTo))
    }

    private fun selectRange(range: DateRangeFilterUiState) {
        _state.update { it.copy(range = range, filterError = null) }
        load()
    }
}
