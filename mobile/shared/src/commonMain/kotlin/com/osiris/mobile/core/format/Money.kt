package com.osiris.mobile.core.format

import kotlin.math.abs
import kotlin.math.round

/** pt-BR money helpers (no platform dependency). */
object Money {
    /** Formats a value as "R$ 1.234,56" (negative as "-R$ ...,..."). */
    fun brl(value: Double): String {
        val cents = round(abs(value) * 100).toLong()
        val intPart = (cents / 100).toString()
        val decPart = (cents % 100).toString().padStart(2, '0')
        val sign = if (value < 0) "-" else ""
        return "${sign}R$ ${groupThousands(intPart)},$decPart"
    }

    /** Parses user input ("1234,56" or "1234.56") into a Double, or null if invalid. */
    fun parse(text: String): Double? = text.trim().replace(',', '.').toDoubleOrNull()

    private fun groupThousands(digits: String): String {
        val sb = StringBuilder()
        val length = digits.length
        for (index in 0 until length) {
            if (index > 0 && (length - index) % 3 == 0) sb.append('.')
            sb.append(digits[index])
        }
        return sb.toString()
    }
}
