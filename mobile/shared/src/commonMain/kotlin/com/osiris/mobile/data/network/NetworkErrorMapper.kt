package com.osiris.mobile.data.network

import com.osiris.mobile.core.result.OsirisError
import com.osiris.mobile.data.dto.ErrorEnvelope
import com.osiris.mobile.data.dto.ProblemDetails
import com.osiris.mobile.data.remote.osirisJson
import io.ktor.client.plugins.ResponseException
import io.ktor.client.statement.bodyAsText
import io.ktor.http.HttpStatusCode

/** Maps Ktor failures into a UI-facing [OsirisError], parsing the server's ProblemDetails/envelope. */
object NetworkErrorMapper {
    suspend fun map(throwable: Throwable): OsirisError {
        if (throwable is ResponseException) {
            val response = throwable.response
            val body = runCatching { response.bodyAsText() }.getOrNull()
            val parsed = body?.let { parse(it) }
            if (parsed != null) {
                return parsed
            }
            if (response.status == HttpStatusCode.Unauthorized) {
                return OsirisError("E-mail ou senha inválidos.")
            }
            return OsirisError("Ocorreu um erro inesperado. Tente novamente.")
        }
        return OsirisError("Não foi possível conectar. Verifique sua conexão e tente novamente.")
    }

    private fun parse(body: String): OsirisError? {
        runCatching {
            val problem = osirisJson.decodeFromString<ProblemDetails>(body)
            val fieldErrors = problem.errors
                ?.mapNotNull { (key, messages) -> messages.firstOrNull()?.let { lowerFirst(key) to it } }
                ?.toMap()
                .orEmpty()
            if (fieldErrors.isNotEmpty()) {
                return OsirisError(fieldErrors.values.first(), fieldErrors)
            }
            if (!problem.title.isNullOrBlank()) {
                return OsirisError(problem.title)
            }
        }
        runCatching {
            val envelope = osirisJson.decodeFromString<ErrorEnvelope>(body)
            if (envelope.errors.isNotEmpty()) {
                val fieldErrors = envelope.errors
                    .filter { !it.field.isNullOrBlank() }
                    .associate { lowerFirst(it.field!!) to it.message }
                return OsirisError(envelope.errors.first().message, fieldErrors)
            }
        }
        return null
    }

    private fun lowerFirst(value: String): String =
        if (value.isEmpty()) value else value.replaceFirstChar { it.lowercaseChar() }
}
