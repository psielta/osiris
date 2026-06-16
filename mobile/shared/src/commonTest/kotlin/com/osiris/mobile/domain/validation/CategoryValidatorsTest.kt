package com.osiris.mobile.domain.validation

import kotlin.test.Test
import kotlin.test.assertNotNull
import kotlin.test.assertNull

class CategoryValidatorsTest {

    @Test
    fun name_accepts_valid() {
        assertNull(CategoryValidators.name("Aluguel"))
    }

    @Test
    fun name_rejects_blank() {
        assertNotNull(CategoryValidators.name(""))
    }

    @Test
    fun name_rejects_too_long() {
        assertNotNull(CategoryValidators.name("x".repeat(101)))
    }

    @Test
    fun color_accepts_null() {
        assertNull(CategoryValidators.color(null))
    }

    @Test
    fun color_accepts_valid_hex() {
        assertNull(CategoryValidators.color("#F59E0B"))
    }

    @Test
    fun color_rejects_invalid() {
        assertNotNull(CategoryValidators.color("vermelho"))
    }
}
