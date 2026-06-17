package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.AuthTokensDto
import com.osiris.mobile.data.dto.ForgotPasswordRequest
import com.osiris.mobile.data.dto.LoginRequest
import com.osiris.mobile.data.dto.LogoutRequest
import com.osiris.mobile.data.dto.RegisterRequest
import com.osiris.mobile.data.dto.UserProfileDto
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.contentType

class AuthApi(
    private val plainClient: HttpClient,
    private val authClient: HttpClient,
    private val config: ApiConfig,
) {
    suspend fun login(request: LoginRequest): AuthTokensDto =
        plainClient.post(url("login")) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun register(request: RegisterRequest): AuthTokensDto =
        plainClient.post(url("register")) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun forgotPassword(request: ForgotPasswordRequest) {
        plainClient.post(url("forgot-password")) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun me(): UserProfileDto =
        authClient.get(url("me")).body()

    suspend fun logout(request: LogoutRequest) {
        authClient.post(url("logout")) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    private fun url(path: String): String = "${config.baseUrl}api/v1/auth/$path"
}
