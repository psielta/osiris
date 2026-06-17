package com.osiris.mobile.presentation.bills

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Bill
import com.osiris.mobile.domain.repository.BillRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import kotlinx.datetime.DatePeriod
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.plus
import kotlinx.datetime.todayIn

data class BillsListUiState(
    val month: Int = currentMonth(),
    val year: Int = currentYear(),
    val bills: List<Bill> = emptyList(),
    val isLoading: Boolean = true,
    val isUpdating: Boolean = false,
    val error: String? = null,
)

sealed interface BillsListEvent {
    data class ShowMessage(val message: String) : BillsListEvent
}

class BillsListViewModel(
    private val billRepository: BillRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(BillsListUiState())
    val state: StateFlow<BillsListUiState> = _state.asStateFlow()

    private val _events = Channel<BillsListEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    fun load() {
        val current = _state.value
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = billRepository.list(current.month, current.year)) {
                is OsirisResult.Success -> _state.update { it.copy(bills = result.value, isLoading = false) }
                is OsirisResult.Failure -> _state.update { it.copy(isLoading = false, error = result.error.message) }
            }
        }
    }

    fun previousMonth() = moveMonth(-1)

    fun nextMonth() = moveMonth(1)

    fun markPending(id: String) {
        _state.update { it.copy(isUpdating = true) }
        viewModelScope.launch {
            when (val result = billRepository.markPending(id)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isUpdating = false) }
                    load()
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isUpdating = false) }
                    _events.send(BillsListEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    private fun moveMonth(delta: Int) {
        val current = _state.value
        val date = LocalDate(current.year, current.month, 1).plus(DatePeriod(months = delta))
        _state.update { it.copy(month = date.monthNumber, year = date.year) }
        load()
    }
}

private fun currentDate() = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo"))
private fun currentMonth(): Int = currentDate().monthNumber
private fun currentYear(): Int = currentDate().year
