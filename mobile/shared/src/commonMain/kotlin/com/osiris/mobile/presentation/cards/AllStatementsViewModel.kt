package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.CreditCardStatementOverview
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AllStatementsUiState(
    val statements: List<CreditCardStatementOverview> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
)

class AllStatementsViewModel(
    private val cardRepository: CardRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(AllStatementsUiState())
    val state: StateFlow<AllStatementsUiState> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = cardRepository.listAllStatements()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(statements = result.value, isLoading = false)
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }
}
