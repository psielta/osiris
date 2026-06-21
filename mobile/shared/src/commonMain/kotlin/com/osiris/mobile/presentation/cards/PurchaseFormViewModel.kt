package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.domain.model.PurchasePreview
import com.osiris.mobile.domain.repository.CardRepository
import com.osiris.mobile.domain.repository.CategoryRepository
import com.osiris.mobile.domain.validation.CardValidators
import kotlinx.coroutines.Job
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import kotlinx.datetime.TimeZone
import kotlinx.datetime.todayIn

data class PurchaseFormUiState(
    val amount: String = "",
    val purchaseDate: String = defaultCardDate(),
    val description: String = "",
    val installments: String = "1",
    val amountIsPerInstallment: Boolean = false,
    val categoryId: String? = null,
    val notes: String = "",
    val categories: List<Category> = emptyList(),
    val preview: PurchasePreview? = null,
    val amountError: String? = null,
    val descriptionError: String? = null,
    val installmentsError: String? = null,
    val categoryError: String? = null,
    val notesError: String? = null,
    val isSubmitting: Boolean = false,
    val isPreviewLoading: Boolean = false,
)

sealed interface PurchaseFormEvent {
    data object NavigateBack : PurchaseFormEvent
    data class ShowMessage(val message: String) : PurchaseFormEvent
}

class PurchaseFormViewModel(
    private val cardRepository: CardRepository,
    private val categoryRepository: CategoryRepository,
    private val cardId: String,
) : ViewModel() {

    private var previewJob: Job? = null

    private val _state = MutableStateFlow(PurchaseFormUiState())
    val state: StateFlow<PurchaseFormUiState> = _state.asStateFlow()

    private val _events = Channel<PurchaseFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        loadCategories()
    }

    private fun loadCategories() {
        viewModelScope.launch {
            when (val result = categoryRepository.list()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(categories = result.value.filter { category -> category.isActive && category.type == CategoryType.Expense })
                }

                is OsirisResult.Failure -> _events.send(PurchaseFormEvent.ShowMessage(result.error.message))
            }
        }
    }

    fun onAmountChange(value: String) = _state.update { it.copy(amount = value, amountError = null) }.also { schedulePreview() }
    fun onAmountModeChange(perInstallment: Boolean) = _state.update { it.copy(amountIsPerInstallment = perInstallment) }.also { schedulePreview() }
    fun onPurchaseDateChange(value: String) = _state.update { it.copy(purchaseDate = value) }.also { schedulePreview() }
    fun onDescriptionChange(value: String) = _state.update { it.copy(description = value, descriptionError = null) }
    fun onInstallmentsChange(value: String) = _state.update { it.copy(installments = value, installmentsError = null) }.also { schedulePreview() }
    fun onCategoryChange(value: String?) = _state.update { it.copy(categoryId = value, categoryError = null) }
    fun onNotesChange(value: String) = _state.update { it.copy(notes = value, notesError = null) }

    fun submit() {
        val current = _state.value
        val amountError = CardValidators.positiveMoney(current.amount, "valor")
        val descriptionError = CardValidators.description(current.description.trim())
        val installmentsError = CardValidators.installments(current.installments)
        val categoryError = CardValidators.category(current.categoryId)
        val notesError = CardValidators.notes(current.notes)
        if (
            amountError != null ||
            descriptionError != null ||
            installmentsError != null ||
            categoryError != null ||
            notesError != null
        ) {
            _state.update {
                it.copy(
                    amountError = amountError,
                    descriptionError = descriptionError,
                    installmentsError = installmentsError,
                    categoryError = categoryError,
                    notesError = notesError,
                )
            }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = cardRepository.createPurchase(
                cardId = cardId,
                categoryId = current.categoryId.orEmpty(),
                description = current.description.trim(),
                totalAmount = effectiveTotal(Money.parse(current.amount) ?: 0.0, current.installments.toInt()),
                purchaseDate = current.purchaseDate,
                installments = current.installments.toInt(),
                notes = current.notes.trim().ifBlank { null },
            )

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(PurchaseFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            amountError = error.fieldErrors["totalAmount"] ?: it.amountError,
                            descriptionError = error.fieldErrors["description"] ?: it.descriptionError,
                            installmentsError = error.fieldErrors["installments"] ?: it.installmentsError,
                            categoryError = error.fieldErrors["categoryId"] ?: it.categoryError,
                            notesError = error.fieldErrors["notes"] ?: it.notesError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(PurchaseFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }

    // When the user typed a per-installment value, the purchase total is value x installments. Round to 2
    // decimals so floating-point artifacts (e.g. 33.33 * 3) don't fail the server's "max 2 decimals" rule.
    private fun effectiveTotal(amount: Double, installments: Int): Double =
        if (_state.value.amountIsPerInstallment) {
            kotlin.math.round(amount * installments * 100.0) / 100.0
        } else {
            amount
        }

    private fun schedulePreview() {
        previewJob?.cancel()
        val current = _state.value
        val amount = Money.parse(current.amount)
        val installments = current.installments.toIntOrNull()
        if (amount == null || amount <= 0.0 || installments == null || installments !in 1..120 || current.purchaseDate.isBlank()) {
            _state.update { it.copy(preview = null, isPreviewLoading = false) }
            return
        }

        previewJob = viewModelScope.launch {
            delay(250)
            _state.update { it.copy(isPreviewLoading = true) }
            when (val result = cardRepository.previewPurchase(cardId, effectiveTotal(amount, installments), current.purchaseDate, installments)) {
                is OsirisResult.Success -> _state.update {
                    it.copy(preview = result.value, isPreviewLoading = false)
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(preview = null, isPreviewLoading = false)
                }
            }
        }
    }

}

private fun defaultCardDate(): String = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo")).toString()
