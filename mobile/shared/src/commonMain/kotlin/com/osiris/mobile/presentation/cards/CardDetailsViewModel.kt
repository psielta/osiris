package com.osiris.mobile.presentation.cards

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.CreditCardDetails
import com.osiris.mobile.domain.model.CreditCardOverview
import com.osiris.mobile.domain.model.CreditCardPurchase
import com.osiris.mobile.domain.model.CreditCardStatement
import com.osiris.mobile.domain.repository.CardRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class CardDetailsUiState(
    val card: CreditCardDetails? = null,
    val overview: CreditCardOverview? = null,
    val currentStatement: CreditCardStatement? = null,
    val purchases: List<CreditCardPurchase> = emptyList(),
    val statements: List<CreditCardStatement> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
)

class CardDetailsViewModel(
    private val cardRepository: CardRepository,
    private val cardId: String,
) : ViewModel() {

    private val _state = MutableStateFlow(CardDetailsUiState())
    val state: StateFlow<CardDetailsUiState> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val cardResult = cardRepository.getCard(cardId)) {
                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = cardResult.error.message)
                }

                is OsirisResult.Success -> {
                    val overview = (cardRepository.overview(cardId) as? OsirisResult.Success)?.value
                    val currentStatement = (cardRepository.currentStatement(cardId) as? OsirisResult.Success)?.value
                    val purchases = (cardRepository.listPurchases(cardId) as? OsirisResult.Success)?.value.orEmpty()
                    val statements = (cardRepository.listStatements(cardId) as? OsirisResult.Success)?.value.orEmpty()
                    _state.update {
                        it.copy(
                            card = cardResult.value,
                            overview = overview,
                            currentStatement = currentStatement,
                            purchases = purchases,
                            statements = statements,
                            isLoading = false,
                        )
                    }
                }
            }
        }
    }
}
