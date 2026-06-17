package com.osiris.mobile.presentation.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.ReportRepository
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

data class ReportsUiState(
    val month: Int = currentMonth(),
    val year: Int = currentYear(),
    val isDownloadingSynthetic: Boolean = false,
    val isDownloadingAnalytic: Boolean = false,
    val error: String? = null,
)

sealed interface ReportsEvent {
    data class OpenPdf(val pdf: StatementPdf) : ReportsEvent
}

class ReportsViewModel(
    private val reportRepository: ReportRepository,
) : ViewModel() {
    private val _state = MutableStateFlow(ReportsUiState())
    val state: StateFlow<ReportsUiState> = _state.asStateFlow()

    private val _events = Channel<ReportsEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    fun previousMonth() = moveMonth(-1)

    fun nextMonth() = moveMonth(1)

    fun downloadSynthetic() {
        val current = _state.value
        _state.update { it.copy(isDownloadingSynthetic = true, error = null) }
        viewModelScope.launch {
            when (val result = reportRepository.downloadCashFlowSyntheticPdf(current.month, current.year)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isDownloadingSynthetic = false) }
                    _events.send(ReportsEvent.OpenPdf(result.value))
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isDownloadingSynthetic = false, error = result.error.message)
                }
            }
        }
    }

    fun downloadAnalytic() {
        val current = _state.value
        _state.update { it.copy(isDownloadingAnalytic = true, error = null) }
        viewModelScope.launch {
            when (val result = reportRepository.downloadCashFlowAnalyticPdf(current.month, current.year)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isDownloadingAnalytic = false) }
                    _events.send(ReportsEvent.OpenPdf(result.value))
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isDownloadingAnalytic = false, error = result.error.message)
                }
            }
        }
    }

    private fun moveMonth(delta: Int) {
        val current = _state.value
        val date = LocalDate(current.year, current.month, 1).plus(DatePeriod(months = delta))
        _state.update { it.copy(month = date.monthNumber, year = date.year, error = null) }
    }
}

private fun currentDate() = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo"))
private fun currentMonth(): Int = currentDate().monthNumber
private fun currentYear(): Int = currentDate().year
