package com.osiris.mobile.presentation.bills

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.BillDetails
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.repository.BillRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import kotlinx.datetime.TimeZone
import kotlinx.datetime.todayIn

data class BillDetailsUiState(
    val bill: BillDetails? = null,
    val paidAt: String = today(),
    val paymentAccountId: String? = null,
    val accounts: List<Account> = emptyList(),
    val isLoading: Boolean = true,
    val isUpdating: Boolean = false,
    val error: String? = null,
)

sealed interface BillDetailsEvent {
    data object NavigateBack : BillDetailsEvent
    data class ShowMessage(val message: String) : BillDetailsEvent
}

class BillDetailsViewModel(
    private val billRepository: BillRepository,
    private val accountRepository: AccountRepository,
    private val billId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(BillDetailsUiState())
    val state: StateFlow<BillDetailsUiState> = _state.asStateFlow()

    private val _events = Channel<BillDetailsEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            val accounts = (accountRepository.list() as? OsirisResult.Success)?.value.orEmpty().filter { it.isActive }
            when (val result = billRepository.get(billId)) {
                is OsirisResult.Success -> _state.update {
                    it.copy(
                        bill = result.value,
                        paymentAccountId = result.value.paymentAccountId?.takeIf { id -> accounts.any { account -> account.id == id } },
                        accounts = accounts,
                        isLoading = false,
                    )
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(accounts = accounts, isLoading = false, error = result.error.message)
                }
            }
        }
    }

    fun onPaidAtChange(value: String) = _state.update { it.copy(paidAt = value) }
    fun onPaymentAccountChange(value: String?) = _state.update { it.copy(paymentAccountId = value) }

    fun pay() {
        val current = _state.value
        if (current.paidAt.isBlank()) {
            viewModelScope.launch { _events.send(BillDetailsEvent.ShowMessage("Informe a data do pagamento.")) }
            return
        }

        _state.update { it.copy(isUpdating = true) }
        viewModelScope.launch {
            when (val result = billRepository.pay(billId, current.paidAt, current.paymentAccountId)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isUpdating = false) }
                    load()
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isUpdating = false) }
                    _events.send(BillDetailsEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    fun markPending() {
        _state.update { it.copy(isUpdating = true) }
        viewModelScope.launch {
            when (val result = billRepository.markPending(billId)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isUpdating = false) }
                    load()
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isUpdating = false) }
                    _events.send(BillDetailsEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    fun delete() {
        _state.update { it.copy(isUpdating = true) }
        viewModelScope.launch {
            when (val result = billRepository.delete(billId)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isUpdating = false) }
                    _events.send(BillDetailsEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isUpdating = false) }
                    _events.send(BillDetailsEvent.ShowMessage(result.error.message))
                }
            }
        }
    }
}

private fun today(): String = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo")).toString()
