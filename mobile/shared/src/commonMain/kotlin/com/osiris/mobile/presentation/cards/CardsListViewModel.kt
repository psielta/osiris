package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.CreditCard
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class CardsListUiState(
    val active: List<CreditCard> = emptyList(),
    val archived: List<CreditCard> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
)

sealed interface CardsListEvent {
    data class ShowMessage(val message: String) : CardsListEvent
}

class CardsListViewModel(
    private val cardRepository: CardRepository,
    private val dataChangeBus: DataChangeBus,
) : ViewModel() {

    private val _state = MutableStateFlow(CardsListUiState())
    val state: StateFlow<CardsListUiState> = _state.asStateFlow()

    private val _events = Channel<CardsListEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Cards) { load() }
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = cardRepository.listCards()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(
                        active = result.value.filter { card -> card.isActive },
                        archived = result.value.filter { card -> !card.isActive },
                        isLoading = false,
                    )
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }

    fun archive(id: String) {
        viewModelScope.launch {
            when (val result = cardRepository.archiveCard(id)) {
                is OsirisResult.Success -> Unit
                is OsirisResult.Failure -> _events.send(CardsListEvent.ShowMessage(result.error.message))
            }
        }
    }
}
