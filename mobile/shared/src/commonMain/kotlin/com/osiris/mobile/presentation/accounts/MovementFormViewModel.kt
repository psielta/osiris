package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.repository.CategoryRepository
import com.osiris.mobile.domain.validation.AccountValidators
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

data class MovementFormUiState(
    val type: MovementType = MovementType.Income,
    val amount: String = "",
    val occurredOn: String = "",
    val description: String = "",
    val categoryId: String? = null,
    val notes: String = "",
    val categories: List<Category> = emptyList(),
    val amountError: String? = null,
    val descriptionError: String? = null,
    val isSubmitting: Boolean = false,
)

sealed interface MovementFormEvent {
    data object NavigateBack : MovementFormEvent
    data class ShowMessage(val message: String) : MovementFormEvent
}

class MovementFormViewModel(
    private val accountRepository: AccountRepository,
    private val categoryRepository: CategoryRepository,
    private val accountId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(MovementFormUiState(occurredOn = today()))
    val state: StateFlow<MovementFormUiState> = _state.asStateFlow()

    private val _events = Channel<MovementFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        loadCategories()
    }

    private fun loadCategories() {
        viewModelScope.launch {
            val result = categoryRepository.list()
            if (result is OsirisResult.Success) {
                _state.update { it.copy(categories = result.value.filter { category -> category.isActive }) }
            }
        }
    }

    fun onTypeChange(value: MovementType) = _state.update { it.copy(type = value) }
    fun onAmountChange(value: String) = _state.update { it.copy(amount = value, amountError = null) }
    fun onOccurredOnChange(value: String) = _state.update { it.copy(occurredOn = value) }
    fun onDescriptionChange(value: String) = _state.update { it.copy(description = value, descriptionError = null) }
    fun onCategoryChange(value: String?) = _state.update { it.copy(categoryId = value) }
    fun onNotesChange(value: String) = _state.update { it.copy(notes = value) }

    fun submit() {
        val current = _state.value
        val amountError = AccountValidators.amount(current.amount)
        val descriptionError = AccountValidators.description(current.description.trim())
        if (amountError != null || descriptionError != null) {
            _state.update { it.copy(amountError = amountError, descriptionError = descriptionError) }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = accountRepository.createMovement(
                accountId = accountId,
                type = current.type,
                amount = Money.parse(current.amount) ?: 0.0,
                occurredOn = current.occurredOn,
                description = current.description.trim(),
                categoryId = current.categoryId,
                notes = current.notes.trim().ifBlank { null },
            )

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(MovementFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            amountError = error.fieldErrors["amount"] ?: it.amountError,
                            descriptionError = error.fieldErrors["description"] ?: it.descriptionError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(MovementFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }

    private companion object {
        fun today(): String = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo")).toString()
    }
}
