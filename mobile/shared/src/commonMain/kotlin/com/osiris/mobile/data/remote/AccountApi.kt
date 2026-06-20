package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.AccountEditDto
import com.osiris.mobile.data.dto.AccountListItemDto
import com.osiris.mobile.data.dto.CreateAccountRequest
import com.osiris.mobile.data.dto.CreateMovementRequest
import com.osiris.mobile.data.dto.CreatedIdResponse
import com.osiris.mobile.data.dto.ImportOfxStatementRequest
import com.osiris.mobile.data.dto.OfxImportPreviewDto
import com.osiris.mobile.data.dto.OfxImportResultDto
import com.osiris.mobile.data.dto.StatementDto
import com.osiris.mobile.data.dto.UpdateAccountRequest
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.forms.MultiPartFormDataContent
import io.ktor.client.request.forms.formData
import io.ktor.client.request.get
import io.ktor.client.request.post
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.http.ContentType
import io.ktor.http.Headers
import io.ktor.http.HttpHeaders
import io.ktor.http.contentType

class AccountApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/accounts"

    suspend fun list(): List<AccountListItemDto> =
        authClient.get(base).body()

    suspend fun get(id: String): AccountEditDto =
        authClient.get("$base/$id").body()

    suspend fun create(request: CreateAccountRequest): CreatedIdResponse =
        authClient.post(base) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun update(id: String, request: UpdateAccountRequest) {
        authClient.put("$base/$id") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun archive(id: String) {
        authClient.post("$base/$id/archive")
    }

    suspend fun statement(id: String): StatementDto =
        authClient.get("$base/$id/statement").body()

    suspend fun downloadStatementPdf(id: String): PdfDownloadResponse {
        val response: HttpResponse = authClient.get("$base/$id/pdf")
        val contentDisposition = response.headers[HttpHeaders.ContentDisposition]
        return PdfDownloadResponse(
            fileName = contentDisposition.fileNameFromDisposition() ?: "extrato-$id.pdf",
            contentType = response.headers[HttpHeaders.ContentType] ?: "application/pdf",
            bytes = response.body(),
        )
    }

    suspend fun createMovement(accountId: String, request: CreateMovementRequest): CreatedIdResponse =
        authClient.post("$base/$accountId/movements") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun previewOfxImport(accountId: String, fileName: String, bytes: ByteArray): OfxImportPreviewDto =
        authClient.post("$base/$accountId/movements/import/preview") {
            setBody(
                MultiPartFormDataContent(
                    formData {
                        append(
                            "file",
                            bytes,
                            Headers.build {
                                append(HttpHeaders.ContentType, "application/octet-stream")
                                append(HttpHeaders.ContentDisposition, "filename=\"$fileName\"")
                            },
                        )
                    },
                ),
            )
        }.body()

    suspend fun confirmOfxImport(accountId: String, request: ImportOfxStatementRequest): OfxImportResultDto =
        authClient.post("$base/$accountId/movements/import") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    private fun String?.fileNameFromDisposition(): String? {
        if (this.isNullOrBlank()) return null
        return split(';')
            .map { it.trim() }
            .firstOrNull { it.startsWith("filename=", ignoreCase = true) }
            ?.substringAfter('=')
            ?.trim()
            ?.trim('"')
            ?.takeIf { it.isNotBlank() }
    }
}
