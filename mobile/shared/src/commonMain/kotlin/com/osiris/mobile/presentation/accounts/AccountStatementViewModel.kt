package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.AccountRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AccountStatementUiState(
    val statement: AccountStatement? = null,
    val isLoading: Boolean = true,
    val isDownloadingPdf: Boolean = false,
    val error: String? = null,
)

sealed interface AccountStatementEvent {
    data class ShowMessage(val message: String) : AccountStatementEvent
    data class OpenPdf(val pdf: StatementPdf) : AccountStatementEvent
}

class AccountStatementViewModel(
    private val accountRepository: AccountRepository,
    private val dataChangeBus: DataChangeBus,
    private val accountId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(AccountStatementUiState())
    val state: StateFlow<AccountStatementUiState> = _state.asStateFlow()

    private val _events = Channel<AccountStatementEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Accounts) { load() }
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = accountRepository.statement(accountId)) {
                is OsirisResult.Success -> _state.update { it.copy(statement = result.value, isLoading = false) }
                is OsirisResult.Failure -> _state.update { it.copy(isLoading = false, error = result.error.message) }
            }
        }
    }

    fun downloadPdf() {
        _state.update { it.copy(isDownloadingPdf = true) }
        viewModelScope.launch {
            when (val result = accountRepository.downloadStatementPdf(accountId)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isDownloadingPdf = false) }
                    _events.send(AccountStatementEvent.OpenPdf(result.value))
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isDownloadingPdf = false) }
                    _events.send(AccountStatementEvent.ShowMessage(result.error.message))
                }
            }
        }
    }
}
