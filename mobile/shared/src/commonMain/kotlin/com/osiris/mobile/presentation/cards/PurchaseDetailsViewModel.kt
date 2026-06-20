package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.CreditCardPurchaseDetails
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class PurchaseDetailsUiState(
    val purchase: CreditCardPurchaseDetails? = null,
    val isLoading: Boolean = true,
    val isDeleting: Boolean = false,
    val error: String? = null,
)

sealed interface PurchaseDetailsEvent {
    data object NavigateBack : PurchaseDetailsEvent
    data class ShowMessage(val message: String) : PurchaseDetailsEvent
}

class PurchaseDetailsViewModel(
    private val cardRepository: CardRepository,
    private val dataChangeBus: DataChangeBus,
    private val cardId: String,
    private val purchaseId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(PurchaseDetailsUiState())
    val state: StateFlow<PurchaseDetailsUiState> = _state.asStateFlow()

    private val _events = Channel<PurchaseDetailsEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Cards) { load() }
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = cardRepository.getPurchase(cardId, purchaseId)) {
                is OsirisResult.Success -> _state.update { it.copy(purchase = result.value, isLoading = false) }
                is OsirisResult.Failure -> _state.update { it.copy(isLoading = false, error = result.error.message) }
            }
        }
    }

    fun delete() {
        _state.update { it.copy(isDeleting = true) }
        viewModelScope.launch {
            when (val result = cardRepository.deletePurchase(cardId, purchaseId)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isDeleting = false) }
                    _events.send(PurchaseDetailsEvent.NavigateBack)
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isDeleting = false) }
                    _events.send(PurchaseDetailsEvent.ShowMessage(result.error.message))
                }
            }
        }
    }
}
