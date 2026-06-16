package com.osiris.mobile.core.format

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class MoneyTest {

    @Test
    fun brl_formats_positive_with_grouping() {
        assertEquals("R$ 1.234,56", Money.brl(1234.56))
    }

    @Test
    fun brl_formats_small_and_zero() {
        assertEquals("R$ 7,00", Money.brl(7.0))
        assertEquals("R$ 0,00", Money.brl(0.0))
    }

    @Test
    fun brl_formats_negative() {
        assertEquals("-R$ 30,00", Money.brl(-30.0))
    }

    @Test
    fun parse_accepts_comma_and_dot() {
        assertEquals(1234.56, Money.parse("1234,56"))
        assertEquals(1234.56, Money.parse("1234.56"))
    }

    @Test
    fun parse_rejects_invalid() {
        assertNull(Money.parse("abc"))
    }
}
