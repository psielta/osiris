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

data class CardFormUiState(
    val name: String = "",
    val limit: String = "",
    val closingDay: String = "",
    val dueDay: String = "",
    val paymentAccountId: String? = null,
    val paymentAccounts: List<Account> = emptyList(),
    val nameError: String? = null,
    val limitError: String? = null,
    val closingDayError: String? = null,
    val dueDayError: String? = null,
    val paymentAccountError: String? = null,
    val isSubmitting: Boolean = false,
    val isLoading: Boolean = false,
    val isEdit: Boolean = false,
)

sealed interface CardFormEvent {
    data object NavigateBack : CardFormEvent
    data class ShowMessage(val message: String) : CardFormEvent
}

class CardFormViewModel(
    private val cardRepository: CardRepository,
    private val accountRepository: AccountRepository,
    private val cardId: String?,
) : ViewModel() {

    private var allAccounts: List<Account> = emptyList()

    private val _state = MutableStateFlow(CardFormUiState(isEdit = cardId != null, isLoading = true))
    val state: StateFlow<CardFormUiState> = _state.asStateFlow()

    private val _events = Channel<CardFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    private fun load() {
        viewModelScope.launch {
            val accountsResult = accountRepository.list()
            allAccounts = if (accountsResult is OsirisResult.Success) accountsResult.value else emptyList()

            if (cardId == null) {
                _state.update { it.copy(paymentAccounts = activeAccounts(null), isLoading = false) }
                if (accountsResult is OsirisResult.Failure) {
                    _events.send(CardFormEvent.ShowMessage(accountsResult.error.message))
                }
                return@launch
            }

            when (val cardResult = cardRepository.getCard(cardId)) {
                is OsirisResult.Success -> {
                    val card = cardResult.value
                    _state.update {
                        it.copy(
                            name = card.name,
                            limit = card.limit.toInput(),
                            closingDay = card.closingDay.toString(),
                            dueDay = card.dueDay.toString(),
                            paymentAccountId = card.paymentAccountId,
                            paymentAccounts = activeAccounts(card.paymentAccountId),
                            isLoading = false,
                        )
                    }
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isLoading = false) }
                    _events.send(CardFormEvent.ShowMessage(cardResult.error.message))
                    _events.send(CardFormEvent.NavigateBack)
                }
            }
        }
    }

    fun onNameChange(value: String) = _state.update { it.copy(name = value, nameError = null) }
    fun onLimitChange(value: String) = _state.update { it.copy(limit = value, limitError = null) }
    fun onClosingDayChange(value: String) = _state.update { it.copy(closingDay = value, closingDayError = null) }
    fun onDueDayChange(value: String) = _state.update { it.copy(dueDay = value, dueDayError = null) }
    fun onPaymentAccountChange(value: String?) = _state.update { it.copy(paymentAccountId = value, paymentAccountError = null) }

    fun submit() {
        val current = _state.value
        val nameError = CardValidators.name(current.name.trim())
        val limitError = CardValidators.money(current.limit, "limite")
        val closingDayError = CardValidators.day(current.closingDay, "dia de fechamento")
        val dueDayError = CardValidators.day(current.dueDay, "dia de vencimento")
        if (nameError != null || limitError != null || closingDayError != null || dueDayError != null) {
            _state.update {
                it.copy(
                    nameError = nameError,
                    limitError = limitError,
                    closingDayError = closingDayError,
                    dueDayError = dueDayError,
                )
            }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = if (cardId == null) {
                cardRepository.createCard(
                    current.name.trim(),
                    Money.parse(current.limit) ?: 0.0,
                    current.closingDay.toInt(),
                    current.dueDay.toInt(),
                    current.paymentAccountId,
                )
            } else {
                cardRepository.updateCard(
                    cardId,
                    current.name.trim(),
                    Money.parse(current.limit) ?: 0.0,
                    current.closingDay.toInt(),
                    current.dueDay.toInt(),
                    current.paymentAccountId,
                )
            }

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(CardFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            nameError = error.fieldErrors["name"] ?: it.nameError,
                            limitError = error.fieldErrors["limit"] ?: it.limitError,
                            closingDayError = error.fieldErrors["closingDay"] ?: it.closingDayError,
                            dueDayError = error.fieldErrors["dueDay"] ?: it.dueDayError,
                            paymentAccountError = error.fieldErrors["paymentAccountId"] ?: it.paymentAccountError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(CardFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }

    private fun activeAccounts(selectedId: String?): List<Account> {
        val active = allAccounts.filter { it.isActive }.toMutableList()
        if (selectedId != null && active.none { it.id == selectedId }) {
            allAccounts.firstOrNull { it.id == selectedId }?.let { active.add(0, it) }
        }
        return active
    }

    private fun Double.toInput(): String =
        if (this % 1.0 == 0.0) toLong().toString() else toString()
}
