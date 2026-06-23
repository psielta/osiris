package com.osiris.mobile.data.remote

import com.osiris.mobile.data.session.SessionManager
import io.ktor.client.HttpClient
import io.ktor.client.plugins.auth.Auth
import io.ktor.client.plugins.auth.providers.BearerTokens
import io.ktor.client.plugins.auth.providers.bearer
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.logging.LogLevel
import io.ktor.client.plugins.logging.Logging
import io.ktor.client.plugins.websocket.WebSockets
import io.ktor.serialization.kotlinx.json.json

/** Anonymous client (no bearer) used for login/register/refresh. */
internal fun buildPlainClient(): HttpClient = HttpClient(osirisHttpEngine()) {
    // Non-2xx throws a ResponseException so NetworkErrorMapper can parse the ProblemDetails body.
    expectSuccess = true
    install(ContentNegotiation) { json(osirisJson) }
    install(Logging) { level = LogLevel.INFO }
}

/**
 * Authenticated client used for protected calls. The Bearer plugin attaches the access token and, on
 * a 401, refreshes (single-flight, with rotation) through [SessionManager] and retries the request.
 */
internal fun buildAuthClient(session: SessionManager): HttpClient = HttpClient(osirisHttpEngine()) {
    // Non-2xx throws a ResponseException so NetworkErrorMapper can parse the ProblemDetails body.
    // The Bearer plugin still intercepts 401 for token refresh before this validator sees the response.
    expectSuccess = true
    install(ContentNegotiation) { json(osirisJson) }
    install(Logging) { level = LogLevel.INFO }
    install(Auth) {
        bearer {
            loadTokens {
                session.currentBundle()?.let { BearerTokens(it.accessToken, it.refreshToken) }
            }
            refreshTokens {
                session.refresh(oldTokens?.refreshToken)
                    ?.let { BearerTokens(it.accessToken, it.refreshToken) }
            }
        }
    }
}

/**
 * Bare client with only the WebSockets plugin, used by the realtime voice relay. It carries no Bearer
 * plugin: the JWT is attached as an explicit Authorization header on each handshake (see VoiceClient),
 * because a 401 during a WebSocket upgrade cannot be transparently retried the way a request can.
 */
internal fun buildWebSocketClient(): HttpClient = HttpClient(osirisHttpEngine()) {
    install(WebSockets)
}
