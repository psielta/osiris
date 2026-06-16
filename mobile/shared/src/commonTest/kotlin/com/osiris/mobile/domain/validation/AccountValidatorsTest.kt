package com.osiris.mobile.domain.validation

import kotlin.test.Test
import kotlin.test.assertNotNull
import kotlin.test.assertNull

class AccountValidatorsTest {

    @Test
    fun name_accepts_valid() {
        assertNull(AccountValidators.name("Banco"))
    }

    @Test
    fun name_rejects_blank() {
        assertNotNull(AccountValidators.name(""))
    }

    @Test
    fun initialBalance_accepts_number() {
        assertNull(AccountValidators.initialBalance("200,50"))
    }

    @Test
    fun initialBalance_rejects_blank_and_invalid() {
        assertNotNull(AccountValidators.initialBalance(""))
        assertNotNull(AccountValidators.initialBalance("abc"))
    }

    @Test
    fun amount_rejects_zero_and_negative() {
        assertNotNull(AccountValidators.amount("0"))
        assertNotNull(AccountValidators.amount("-5"))
    }

    @Test
    fun amount_accepts_positive() {
        assertNull(AccountValidators.amount("12,34"))
    }

    @Test
    fun description_rejects_blank() {
        assertNotNull(AccountValidators.description(""))
    }
}
