package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

/** ASP.NET Core RFC7807 ValidationProblemDetails shape. */
@Serializable
data class ProblemDetails(
    val title: String? = null,
    val detail: String? = null,
    val status: Int? = null,
    val errors: Map<String, List<String>>? = null,
)

@Serializable
data class ApiError(
    val message: String,
    val field: String? = null,
    val code: String? = null,
)

@Serializable
data class ErrorEnvelope(
    val errors: List<ApiError> = emptyList(),
)
