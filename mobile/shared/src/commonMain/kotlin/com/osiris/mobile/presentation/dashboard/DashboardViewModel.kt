package com.osiris.mobile.presentation.dashboard

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.DashboardSummary
import com.osiris.mobile.domain.repository.DashboardRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import kotlinx.datetime.DatePeriod
import kotlinx.datetime.TimeZone
import kotlinx.datetime.plus
import kotlinx.datetime.todayIn

data class DashboardUiState(
    val month: Int = currentMonth(),
    val year: Int = currentYear(),
    val summary: DashboardSummary? = null,
    val isLoading: Boolean = true,
    val error: String? = null,
)

class DashboardViewModel(
    private val dashboardRepository: DashboardRepository,
    private val dataChangeBus: DataChangeBus,
) : ViewModel() {

    private val _state = MutableStateFlow(DashboardUiState())
    val state: StateFlow<DashboardUiState> = _state.asStateFlow()

    init {
        load()
        observeDataChanges(dataChangeBus, DataScope.Dashboard) { load() }
    }

    fun load() {
        val current = _state.value
        _state.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            when (val result = dashboardRepository.get(current.month, current.year)) {
                is OsirisResult.Success -> _state.update {
                    it.copy(summary = result.value, isLoading = false)
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoading = false, error = result.error.message)
                }
            }
        }
    }

    fun previousMonth() = moveMonth(-1)

    fun nextMonth() = moveMonth(1)

    private fun moveMonth(delta: Int) {
        val current = _state.value
        val date = kotlinx.datetime.LocalDate(current.year, current.month, 1).plus(DatePeriod(months = delta))
        _state.update { it.copy(month = date.monthNumber, year = date.year) }
        load()
    }
}

private fun currentDate() = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo"))
private fun currentMonth(): Int = currentDate().monthNumber
private fun currentYear(): Int = currentDate().year
