package com.osiris.mobile.presentation.bills

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.repository.BillRepository
import com.osiris.mobile.domain.repository.CategoryRepository
import com.osiris.mobile.domain.validation.BillValidators
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

data class BillFormUiState(
    val isEdit: Boolean = false,
    val description: String = "",
    val amount: String = "",
    val dueDate: String = today(),
    val categoryId: String? = null,
    val paymentAccountId: String? = null,
    val notes: String = "",
    val categories: List<Category> = emptyList(),
    val accounts: List<Account> = emptyList(),
    val descriptionError: String? = null,
    val amountError: String? = null,
    val dueDateError: String? = null,
    val categoryError: String? = null,
    val notesError: String? = null,
    val isLoading: Boolean = true,
    val isSubmitting: Boolean = false,
)

sealed interface BillFormEvent {
    data object NavigateBack : BillFormEvent
    data class ShowMessage(val message: String) : BillFormEvent
}

class BillFormViewModel(
    private val billRepository: BillRepository,
    private val categoryRepository: CategoryRepository,
    private val accountRepository: AccountRepository,
    private val billId: String?,
) : ViewModel() {

    private val _state = MutableStateFlow(BillFormUiState(isEdit = billId != null))
    val state: StateFlow<BillFormUiState> = _state.asStateFlow()

    private val _events = Channel<BillFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    private fun load() {
        viewModelScope.launch {
            val categories = (categoryRepository.list() as? OsirisResult.Success)
                ?.value
                .orEmpty()
                .filter { it.isActive && it.type == CategoryType.Expense }
            val accounts = (accountRepository.list() as? OsirisResult.Success)
                ?.value
                .orEmpty()
                .filter { it.isActive }
            val bill = billId?.let { (billRepository.get(it) as? OsirisResult.Success)?.value }

            _state.update {
                it.copy(
                    description = bill?.description ?: it.description,
                    amount = bill?.amount?.toInput() ?: it.amount,
                    dueDate = bill?.dueDate ?: it.dueDate,
                    categoryId = bill?.categoryId ?: it.categoryId,
                    paymentAccountId = bill?.paymentAccountId ?: it.paymentAccountId,
                    notes = bill?.notes.orEmpty(),
                    categories = categories,
                    accounts = accounts,
                    isLoading = false,
                )
            }

            if (billId != null && bill == null) {
                _events.send(BillFormEvent.ShowMessage("Bill nao encontrada."))
                _events.send(BillFormEvent.NavigateBack)
            }
        }
    }

    fun onDescriptionChange(value: String) = _state.update { it.copy(description = value, descriptionError = null) }
    fun onAmountChange(value: String) = _state.update { it.copy(amount = value, amountError = null) }
    fun onDueDateChange(value: String) = _state.update { it.copy(dueDate = value, dueDateError = null) }
    fun onCategoryChange(value: String?) = _state.update { it.copy(categoryId = value, categoryError = null) }
    fun onPaymentAccountChange(value: String?) = _state.update { it.copy(paymentAccountId = value) }
    fun onNotesChange(value: String) = _state.update { it.copy(notes = value, notesError = null) }

    fun submit() {
        val current = _state.value
        val descriptionError = BillValidators.description(current.description.trim())
        val amountError = BillValidators.amount(current.amount)
        val dueDateError = BillValidators.dueDate(current.dueDate)
        val categoryError = BillValidators.category(current.categoryId)
        val notesError = BillValidators.notes(current.notes)
        if (
            descriptionError != null ||
            amountError != null ||
            dueDateError != null ||
            categoryError != null ||
            notesError != null
        ) {
            _state.update {
                it.copy(
                    descriptionError = descriptionError,
                    amountError = amountError,
                    dueDateError = dueDateError,
                    categoryError = categoryError,
                    notesError = notesError,
                )
            }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val amount = Money.parse(current.amount) ?: 0.0
            val result = if (billId == null) {
                billRepository.create(
                    current.description.trim(),
                    amount,
                    current.dueDate,
                    current.categoryId.orEmpty(),
                    current.paymentAccountId,
                    current.notes.trim().ifBlank { null },
                )
            } else {
                billRepository.update(
                    billId,
                    current.description.trim(),
                    amount,
                    current.dueDate,
                    current.categoryId.orEmpty(),
                    current.paymentAccountId,
                    current.notes.trim().ifBlank { null },
                )
            }

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(BillFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            descriptionError = error.fieldErrors["description"] ?: it.descriptionError,
                            amountError = error.fieldErrors["amount"] ?: it.amountError,
                            dueDateError = error.fieldErrors["dueDate"] ?: it.dueDateError,
                            categoryError = error.fieldErrors["categoryId"] ?: it.categoryError,
                            notesError = error.fieldErrors["notes"] ?: it.notesError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(BillFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }
}

private fun today(): String = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo")).toString()

private fun Double.toInput(): String =
    if (this % 1.0 == 0.0) toLong().toString() else toString()
