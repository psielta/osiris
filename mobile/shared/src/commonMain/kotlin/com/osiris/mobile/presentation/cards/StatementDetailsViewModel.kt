package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.CreditCardStatementDetails
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class StatementDetailsUiState(
    val statement: CreditCardStatementDetails? = null,
    val isLoading: Boolean = true,
    val isDownloadingPdf: Boolean = false,
    val error: String? = null,
)

sealed interface StatementDetailsEvent {
    data class ShowMessage(val message: String) : StatementDetailsEvent
    data class OpenPdf(val pdf: StatementPdf) : StatementDetailsEvent
}

class StatementDetailsViewModel(
    private val cardRepository: CardRepository,
    private val cardId: String,
    private val statementId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(StatementDetailsUiState())
    val state: StateFlow<StatementDetailsUiState> = _state.asStateFlow()

    private val _events = Channel<StatementDetailsEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = cardRepository.getStatement(cardId, statementId)) {
                is OsirisResult.Success -> _state.update { it.copy(statement = result.value, isLoading = false) }
                is OsirisResult.Failure -> _state.update { it.copy(isLoading = false, error = result.error.message) }
            }
        }
    }

    fun downloadPdf() {
        _state.update { it.copy(isDownloadingPdf = true) }
        viewModelScope.launch {
            when (val result = cardRepository.downloadStatementPdf(cardId, statementId)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isDownloadingPdf = false) }
                    _events.send(StatementDetailsEvent.OpenPdf(result.value))
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isDownloadingPdf = false) }
                    _events.send(StatementDetailsEvent.ShowMessage(result.error.message))
                }
            }
        }
    }
}
