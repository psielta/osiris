package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.AuthUser

interface AuthRepository {
    suspend fun login(email: String, password: String): OsirisResult<AuthUser>

    suspend fun register(
        tenantName: String,
        fullName: String,
        email: String,
        password: String,
        confirmPassword: String,
    ): OsirisResult<AuthUser>

    suspend fun currentUser(): OsirisResult<AuthUser>

    suspend fun logout()
}
