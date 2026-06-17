package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class LoginRequest(val email: String, val password: String)

@Serializable
data class RegisterRequest(
    val tenantName: String,
    val fullName: String,
    val email: String,
    val password: String,
    val confirmPassword: String,
)

@Serializable
data class ForgotPasswordRequest(val email: String)

@Serializable
data class RefreshRequest(val refreshToken: String)

@Serializable
data class LogoutRequest(val refreshToken: String)

@Serializable
data class AuthUserDto(
    val fullName: String,
    val email: String,
    val tenantName: String,
)

@Serializable
data class AuthTokensDto(
    val accessToken: String,
    val refreshToken: String,
    val tokenType: String = "Bearer",
    val user: AuthUserDto,
)

@Serializable
data class UserProfileDto(
    val email: String,
    val fullName: String,
    val tenantName: String,
)
