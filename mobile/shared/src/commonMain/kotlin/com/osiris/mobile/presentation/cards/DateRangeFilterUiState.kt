package com.osiris.mobile.presentation.cards

import kotlinx.datetime.Clock
import kotlinx.datetime.DatePeriod
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.plus
import kotlinx.datetime.todayIn

data class DateRangeFilterUiState(
    val from: String,
    val to: String,
    val customFrom: String = from,
    val customTo: String = to,
) {
    val label: String
        get() = if (from == to) {
            formatIsoDate(from)
        } else {
            "${formatIsoDate(from)} - ${formatIsoDate(to)}"
        }
}

fun currentMonthRange(): DateRangeFilterUiState {
    val today = currentDate()
    val start = LocalDate(today.year, today.monthNumber, 1)
    return rangeFromStart(start)
}

fun nextMonthRange(): DateRangeFilterUiState {
    val today = currentDate()
    val start = LocalDate(today.year, today.monthNumber, 1).plus(DatePeriod(months = 1))
    return rangeFromStart(start)
}

fun isValidDateRange(from: String, to: String): Boolean =
    runCatching { LocalDate.parse(from) <= LocalDate.parse(to) }.getOrDefault(false)

private fun rangeFromStart(start: LocalDate): DateRangeFilterUiState {
    val end = start.plus(DatePeriod(months = 1)).plus(DatePeriod(days = -1))
    return DateRangeFilterUiState(start.toString(), end.toString())
}

private fun formatIsoDate(iso: String): String =
    runCatching {
        val date = LocalDate.parse(iso)
        "${date.dayOfMonth.toString().padStart(2, '0')}/${date.monthNumber.toString().padStart(2, '0')}/${date.year}"
    }.getOrDefault(iso)

private fun currentDate() = Clock.System.todayIn(TimeZone.of("America/Sao_Paulo"))
