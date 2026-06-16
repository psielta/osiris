package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.repository.AccountRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AccountStatementUiState(
    val statement: AccountStatement? = null,
    val isLoading: Boolean = true,
    val error: String? = null,
)

class AccountStatementViewModel(
    private val accountRepository: AccountRepository,
    private val accountId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(AccountStatementUiState())
    val state: StateFlow<AccountStatementUiState> = _state.asStateFlow()

    init {
        load()
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
}
