package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.validation.AccountValidators
import com.osiris.mobile.core.format.Money
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AccountFormUiState(
    val name: String = "",
    val type: AccountType = AccountType.Checking,
    val initialBalance: String = "",
    val nameError: String? = null,
    val initialBalanceError: String? = null,
    val isSubmitting: Boolean = false,
    val isLoading: Boolean = false,
    val isEdit: Boolean = false,
)

sealed interface AccountFormEvent {
    data object NavigateBack : AccountFormEvent
    data class ShowMessage(val message: String) : AccountFormEvent
}

class AccountFormViewModel(
    private val accountRepository: AccountRepository,
    private val accountId: String?,
) : ViewModel() {

    private val _state = MutableStateFlow(AccountFormUiState(isEdit = accountId != null))
    val state: StateFlow<AccountFormUiState> = _state.asStateFlow()

    private val _events = Channel<AccountFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        if (accountId != null) {
            loadExisting(accountId)
        }
    }

    private fun loadExisting(id: String) {
        _state.update { it.copy(isLoading = true) }
        viewModelScope.launch {
            when (val result = accountRepository.get(id)) {
                is OsirisResult.Success -> _state.update {
                    it.copy(name = result.value.name, type = result.value.type, isLoading = false)
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isLoading = false) }
                    _events.send(AccountFormEvent.ShowMessage(result.error.message))
                    _events.send(AccountFormEvent.NavigateBack)
                }
            }
        }
    }

    fun onNameChange(value: String) = _state.update { it.copy(name = value, nameError = null) }
    fun onTypeChange(value: AccountType) = _state.update { it.copy(type = value) }
    fun onInitialBalanceChange(value: String) = _state.update { it.copy(initialBalance = value, initialBalanceError = null) }

    fun submit() {
        val current = _state.value
        val nameError = AccountValidators.name(current.name.trim())
        // InitialBalance is only set/validated on create (immutable on edit).
        val balanceError = if (current.isEdit) null else AccountValidators.initialBalance(current.initialBalance)
        if (nameError != null || balanceError != null) {
            _state.update { it.copy(nameError = nameError, initialBalanceError = balanceError) }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = if (accountId == null) {
                accountRepository.create(current.name.trim(), current.type, Money.parse(current.initialBalance) ?: 0.0)
            } else {
                accountRepository.update(accountId, current.name.trim(), current.type)
            }

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(AccountFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            nameError = error.fieldErrors["name"] ?: it.nameError,
                            initialBalanceError = error.fieldErrors["initialBalance"] ?: it.initialBalanceError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(AccountFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }
}
