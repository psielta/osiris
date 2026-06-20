package com.osiris.mobile.presentation.categories

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.repository.CategoryRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class CategoriesListUiState(
    val active: List<Category> = emptyList(),
    val archived: List<Category> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
)

sealed interface CategoriesListEvent {
    data class ShowMessage(val message: String) : CategoriesListEvent
}

class CategoriesListViewModel(
    private val categoryRepository: CategoryRepository,
    private val dataChangeBus: DataChangeBus,
) : ViewModel() {

    private val _state = MutableStateFlow(CategoriesListUiState())
    val state: StateFlow<CategoriesListUiState> = _state.asStateFlow()

    private val _events = Channel<CategoriesListEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Categories) { load() }
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = categoryRepository.list()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(
                        active = result.value.filter { category -> category.isActive },
                        archived = result.value.filter { category -> !category.isActive },
                        isLoading = false,
                    )
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }

    fun archive(id: String) = mutate { categoryRepository.archive(id) }

    fun delete(id: String) = mutate { categoryRepository.delete(id) }

    private fun mutate(action: suspend () -> OsirisResult<Unit>) {
        viewModelScope.launch {
            when (val result = action()) {
                is OsirisResult.Success -> Unit
                is OsirisResult.Failure -> _events.send(CategoriesListEvent.ShowMessage(result.error.message))
            }
        }
    }
}
