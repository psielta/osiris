package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.LoginRequest
import com.osiris.mobile.data.dto.LogoutRequest
import com.osiris.mobile.data.dto.RegisterRequest
import com.osiris.mobile.data.network.NetworkErrorMapper
import com.osiris.mobile.data.remote.AuthApi
import com.osiris.mobile.data.session.SessionManager
import com.osiris.mobile.data.session.TokenBundle
import com.osiris.mobile.domain.model.AuthUser
import com.osiris.mobile.domain.repository.AuthRepository

class AuthRepositoryImpl(
    private val api: AuthApi,
    private val session: SessionManager,
) : AuthRepository {

    override suspend fun login(email: String, password: String): OsirisResult<AuthUser> = runAuth {
        val tokens = api.login(LoginRequest(email, password))
        val user = AuthUser(tokens.user.fullName, tokens.user.email, tokens.user.tenantName)
        session.onAuthenticated(TokenBundle(tokens.accessToken, tokens.refreshToken), user)
        user
    }

    override suspend fun register(
        tenantName: String,
        fullName: String,
        email: String,
        password: String,
        confirmPassword: String,
    ): OsirisResult<AuthUser> = runAuth {
        val tokens = api.register(
            RegisterRequest(tenantName, fullName, email, password, confirmPassword),
        )
        val user = AuthUser(tokens.user.fullName, tokens.user.email, tokens.user.tenantName)
        session.onAuthenticated(TokenBundle(tokens.accessToken, tokens.refreshToken), user)
        user
    }

    override suspend fun currentUser(): OsirisResult<AuthUser> = runAuth {
        val profile = api.me()
        AuthUser(profile.fullName, profile.email, profile.tenantName)
    }

    override suspend fun logout() {
        runCatching {
            session.currentBundle()?.let { api.logout(LogoutRequest(it.refreshToken)) }
        }
        // Always clear the local session, regardless of the network result.
        session.signOut()
    }

    private suspend fun runAuth(block: suspend () -> AuthUser): OsirisResult<AuthUser> =
        try {
            OsirisResult.Success(block())
        } catch (throwable: Throwable) {
            OsirisResult.Failure(NetworkErrorMapper.map(throwable))
        }
}
