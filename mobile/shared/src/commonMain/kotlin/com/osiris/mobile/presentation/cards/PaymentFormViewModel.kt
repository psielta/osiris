package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.repository.CardRepository
import com.osiris.mobile.domain.validation.CardValidators
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

data class PaymentFormUiState(
    val amount: String = "",
    val paidAt: String = defaultPaymentDate(),
    val financialAccountId: String? = null,
    val accounts: List<Account> = emptyList(),
    val amountError: String? = null,
    val paidAtError: String? = null,
    val financialAccountError: String? = null,
    val notes: String = "",
    val notesError: String? = null,
    val isLoading: Boolean = true,
    val isSubmitting: Boolean = false,
)

sealed interface PaymentFormEvent {
    data object NavigateBack : PaymentFormEvent
    data class ShowMessage(val message: String) : PaymentFormEvent
}

class PaymentFormViewModel(
    private val cardRepository: CardRepository,
    private val accountRepository: AccountRepository,
    private val cardId: String,
    private val statementId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(PaymentFormUiState())
    val state: StateFlow<PaymentFormUiState> = _state.asStateFlow()

    private val _events = Channel<PaymentFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    private fun load() {
        viewModelScope.launch {
            val card = (cardRepository.getCard(cardId) as? OsirisResult.Success)?.value
            val statement = (cardRepository.getStatement(cardId, statementId) as? OsirisResult.Success)?.value
            val accounts = (accountRepository.list() as? OsirisResult.Success)?.value.orEmpty().filter { it.isActive }
            val defaultAccountId = card?.paymentAccountId?.takeIf { id -> accounts.any { it.id == id } }

            _state.update {
                it.copy(
                    amount = statement?.openBalance?.toInput().orEmpty(),
                    financialAccountId = defaultAccountId,
                    accounts = accounts,
                    isLoading = false,
                )
            }

            if (statement == null) {
                _events.send(PaymentFormEvent.ShowMessage("Fatura nao encontrada."))
                _events.send(PaymentFormEvent.NavigateBack)
            }
        }
    }

    fun onAmountChange(value: String) = _state.update { it.copy(amount = value, amountError = null) }
    fun onPaidAtChange(value: String) = _state.update { it.copy(paidAt = value, paidAtError = null) }
    fun onFinancialAccountChange(value: String?) = _state.update { it.copy(financialAccountId = value, financialAccountError = null) }
    fun onNotesChange(value: String) = _state.update { it.copy(notes = value, notesError = null) }

    fun submit() {
        val current = _state.value
        val amountError = CardValidators.positiveMoney(current.amount, "valor do pagamento")
        val paidAtError = if (current.paidAt.isBlank()) "Informe a data do pagamento." else null
        val notesError = CardValidators.notes(current.notes)
        if (amountError != null || paidAtError != null || notesError != null) {
            _state.update { it.copy(amountError = amountError, paidAtError = paidAtError, notesError = notesError) }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = cardRepository.payStatement(
                cardId = cardId,
                statementId = statementId,
                amount = Money.parse(current.amount) ?: 0.0,
                paidAt = current.paidAt,
                financialAccountId = current.financialAccountId,
                notes = current.notes.trim().ifBlank { null },
            )

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(PaymentFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            amountError = error.fieldErrors["amount"] ?: it.amountError,
                            paidAtError = error.fieldErrors["paidAt"] ?: it.paidAtError,
                            financialAccountError = error.fieldErrors["financialAccountId"] ?: it.financialAccountError,
                            notesError = error.fieldErrors["notes"] ?: it.notesError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(PaymentFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }

    private fun Double.toInput(): String =
        if (this % 1.0 == 0.0) toLong().toString() else toString()

}

private fun defaultPaymentDate(): String = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo")).toString()
