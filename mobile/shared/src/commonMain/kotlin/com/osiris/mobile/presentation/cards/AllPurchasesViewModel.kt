package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.CreditCardPurchaseOverview
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AllPurchasesUiState(
    val purchases: List<CreditCardPurchaseOverview> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
)

class AllPurchasesViewModel(
    private val cardRepository: CardRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(AllPurchasesUiState())
    val state: StateFlow<AllPurchasesUiState> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = cardRepository.listAllPurchases()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(purchases = result.value, isLoading = false)
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }
}
