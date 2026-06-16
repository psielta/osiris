package com.osiris.mobile.domain.validation

import kotlin.test.Test
import kotlin.test.assertNotNull
import kotlin.test.assertNull

class AuthValidatorsTest {

    @Test
    fun email_accepts_valid() {
        assertNull(AuthValidators.email("jane@osiris.test"))
    }

    @Test
    fun email_rejects_blank() {
        assertNotNull(AuthValidators.email(""))
    }

    @Test
    fun email_rejects_malformed() {
        assertNotNull(AuthValidators.email("not-an-email"))
    }

    @Test
    fun password_rejects_too_short() {
        assertNotNull(AuthValidators.password("12345"))
    }

    @Test
    fun password_accepts_min_length() {
        assertNull(AuthValidators.password("password1"))
    }

    @Test
    fun confirm_rejects_mismatch() {
        assertNotNull(AuthValidators.confirmPassword("password1", "different"))
    }

    @Test
    fun confirm_accepts_match() {
        assertNull(AuthValidators.confirmPassword("password1", "password1"))
    }

    @Test
    fun tenantName_rejects_blank() {
        assertNotNull(AuthValidators.tenantName(""))
    }

    @Test
    fun fullName_rejects_blank() {
        assertNotNull(AuthValidators.fullName(""))
    }
}
