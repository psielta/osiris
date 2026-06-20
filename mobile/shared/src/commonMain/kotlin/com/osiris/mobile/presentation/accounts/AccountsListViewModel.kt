package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.repository.AccountRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AccountsListUiState(
    val active: List<Account> = emptyList(),
    val archived: List<Account> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
)

sealed interface AccountsListEvent {
    data class ShowMessage(val message: String) : AccountsListEvent
}

class AccountsListViewModel(
    private val accountRepository: AccountRepository,
    private val dataChangeBus: DataChangeBus,
) : ViewModel() {

    private val _state = MutableStateFlow(AccountsListUiState())
    val state: StateFlow<AccountsListUiState> = _state.asStateFlow()

    private val _events = Channel<AccountsListEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Accounts) { load() }
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = accountRepository.list()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(
                        active = result.value.filter { account -> account.isActive },
                        archived = result.value.filter { account -> !account.isActive },
                        isLoading = false,
                    )
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }

    fun archive(id: String) {
        viewModelScope.launch {
            when (val result = accountRepository.archive(id)) {
                is OsirisResult.Success -> Unit
                is OsirisResult.Failure -> _events.send(AccountsListEvent.ShowMessage(result.error.message))
            }
        }
    }
}
