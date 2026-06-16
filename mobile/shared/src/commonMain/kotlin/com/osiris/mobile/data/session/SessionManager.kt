package com.osiris.mobile.data.session

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.AuthTokensDto
import com.osiris.mobile.data.dto.RefreshRequest
import com.osiris.mobile.domain.model.AuthUser
import com.osiris.mobile.presentation.SessionState
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.contentType
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

/**
 * Owns the in-memory token cache and the session state the UI gate observes. Refresh is single-flight
 * (a [Mutex] serializes it) and uses the anonymous client so it never re-enters the Bearer plugin.
 */
class SessionManager(
    private val tokenStore: TokenStore,
    private val plainClient: HttpClient,
    private val config: ApiConfig,
) {
    private val mutex = Mutex()
    private var cached: TokenBundle? = null

    private val _sessionState = MutableStateFlow<SessionState>(SessionState.Loading)
    val sessionState: StateFlow<SessionState> = _sessionState.asStateFlow()

    suspend fun bootstrap() {
        cached = tokenStore.read()
        _sessionState.value =
            if (cached != null) SessionState.Authenticated(null) else SessionState.Unauthenticated
    }

    fun currentBundle(): TokenBundle? = cached

    suspend fun onAuthenticated(bundle: TokenBundle, user: AuthUser) {
        cached = bundle
        tokenStore.save(bundle)
        _sessionState.value = SessionState.Authenticated(user)
    }

    suspend fun refresh(usedRefreshToken: String?): TokenBundle? = mutex.withLock {
        val current = cached ?: return null
        // Single-flight re-check: if a concurrent caller already rotated past the token that triggered
        // this 401, reuse the fresh bundle instead of consuming the rotated refresh token again.
        if (usedRefreshToken != null && current.refreshToken != usedRefreshToken) {
            return current
        }
        try {
            val response: AuthTokensDto = plainClient.post("${config.baseUrl}api/v1/auth/refresh") {
                contentType(ContentType.Application.Json)
                setBody(RefreshRequest(current.refreshToken))
            }.body()
            val bundle = TokenBundle(response.accessToken, response.refreshToken)
            cached = bundle
            tokenStore.save(bundle)
            bundle
        } catch (throwable: Throwable) {
            clearLocal()
            null
        }
    }

    suspend fun signOut() {
        clearLocal()
    }

    private suspend fun clearLocal() {
        cached = null
        tokenStore.clear()
        _sessionState.value = SessionState.Unauthenticated
    }
}
