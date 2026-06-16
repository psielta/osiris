package com.osiris.mobile.presentation.categories

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.domain.repository.CategoryRepository
import com.osiris.mobile.domain.validation.CategoryValidators
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class CategoryFormUiState(
    val name: String = "",
    val type: CategoryType = CategoryType.Expense,
    val color: String? = CategoryColors.Default,
    val nameError: String? = null,
    val colorError: String? = null,
    val isSubmitting: Boolean = false,
    val isLoading: Boolean = false,
    val isEdit: Boolean = false,
)

sealed interface CategoryFormEvent {
    data object NavigateBack : CategoryFormEvent
    data class ShowMessage(val message: String) : CategoryFormEvent
}

class CategoryFormViewModel(
    private val categoryRepository: CategoryRepository,
    private val categoryId: String?,
) : ViewModel() {

    private val _state = MutableStateFlow(CategoryFormUiState(isEdit = categoryId != null))
    val state: StateFlow<CategoryFormUiState> = _state.asStateFlow()

    private val _events = Channel<CategoryFormEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        if (categoryId != null) {
            loadExisting(categoryId)
        }
    }

    private fun loadExisting(id: String) {
        _state.update { it.copy(isLoading = true) }
        viewModelScope.launch {
            when (val result = categoryRepository.get(id)) {
                is OsirisResult.Success -> _state.update {
                    it.copy(
                        name = result.value.name,
                        type = result.value.type,
                        color = result.value.color,
                        isLoading = false,
                    )
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isLoading = false) }
                    _events.send(CategoryFormEvent.ShowMessage(result.error.message))
                    _events.send(CategoryFormEvent.NavigateBack)
                }
            }
        }
    }

    fun onNameChange(value: String) = _state.update { it.copy(name = value, nameError = null) }

    fun onTypeChange(value: CategoryType) = _state.update { it.copy(type = value) }

    fun onColorChange(value: String?) = _state.update { it.copy(color = value, colorError = null) }

    fun submit() {
        val current = _state.value
        val nameError = CategoryValidators.name(current.name.trim())
        val colorError = CategoryValidators.color(current.color)
        if (nameError != null || colorError != null) {
            _state.update { it.copy(nameError = nameError, colorError = colorError) }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = if (categoryId == null) {
                categoryRepository.create(current.name.trim(), current.type, current.color)
            } else {
                categoryRepository.update(categoryId, current.name.trim(), current.type, current.color)
            }

            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(CategoryFormEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            nameError = error.fieldErrors["name"] ?: it.nameError,
                            colorError = error.fieldErrors["color"] ?: it.colorError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(CategoryFormEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }
}
