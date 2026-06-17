package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.CardDetailsDto
import com.osiris.mobile.data.dto.CardListItemDto
import com.osiris.mobile.data.dto.CardOverviewDto
import com.osiris.mobile.data.dto.CreateCardRequest
import com.osiris.mobile.data.dto.CreatePurchaseRequest
import com.osiris.mobile.data.dto.CreatedIdResponse
import com.osiris.mobile.data.dto.PurchaseDetailsDto
import com.osiris.mobile.data.dto.PurchaseListItemDto
import com.osiris.mobile.data.dto.PurchaseOverviewDto
import com.osiris.mobile.data.dto.PurchasePreviewDto
import com.osiris.mobile.data.dto.RegisterStatementPaymentRequest
import com.osiris.mobile.data.dto.StatementDetailsDto
import com.osiris.mobile.data.dto.StatementListItemDto
import com.osiris.mobile.data.dto.StatementOverviewDto
import com.osiris.mobile.data.dto.UpdateCardRequest
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.delete
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.request.post
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.http.ContentType
import io.ktor.http.HttpHeaders
import io.ktor.http.contentType

class CardApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/cards"

    suspend fun listCards(): List<CardListItemDto> =
        authClient.get(base).body()

    suspend fun getCard(id: String): CardDetailsDto =
        authClient.get("$base/$id").body()

    suspend fun createCard(request: CreateCardRequest): CreatedIdResponse =
        authClient.post(base) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun updateCard(id: String, request: UpdateCardRequest) {
        authClient.put("$base/$id") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun archiveCard(id: String) {
        authClient.post("$base/$id/archive")
    }

    suspend fun overview(cardId: String): CardOverviewDto =
        authClient.get("$base/$cardId/overview").body()

    suspend fun listPurchases(cardId: String): List<PurchaseListItemDto> =
        authClient.get("$base/$cardId/purchases").body()

    suspend fun listAllPurchases(from: String, to: String): List<PurchaseOverviewDto> =
        authClient.get("$base/purchases") {
            parameter("from", from)
            parameter("to", to)
        }.body()

    suspend fun getPurchase(cardId: String, purchaseId: String): PurchaseDetailsDto =
        authClient.get("$base/$cardId/purchases/$purchaseId").body()

    suspend fun previewPurchase(
        cardId: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
    ): PurchasePreviewDto? =
        authClient.get("$base/$cardId/purchases/preview") {
            parameter("totalAmount", totalAmount)
            parameter("purchaseDate", purchaseDate)
            parameter("installments", installments)
        }.body()

    suspend fun createPurchase(cardId: String, request: CreatePurchaseRequest): CreatedIdResponse =
        authClient.post("$base/$cardId/purchases") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun deletePurchase(cardId: String, purchaseId: String) {
        authClient.delete("$base/$cardId/purchases/$purchaseId")
    }

    suspend fun listStatements(cardId: String): List<StatementListItemDto> =
        authClient.get("$base/$cardId/statements").body()

    suspend fun listAllStatements(from: String, to: String): List<StatementOverviewDto> =
        authClient.get("$base/statements") {
            parameter("from", from)
            parameter("to", to)
        }.body()

    suspend fun currentStatement(cardId: String): StatementListItemDto? =
        authClient.get("$base/$cardId/statements/current").body()

    suspend fun getStatement(cardId: String, statementId: String): StatementDetailsDto =
        authClient.get("$base/$cardId/statements/$statementId").body()

    suspend fun payStatement(
        cardId: String,
        statementId: String,
        request: RegisterStatementPaymentRequest,
    ): CreatedIdResponse =
        authClient.post("$base/$cardId/statements/$statementId/payments") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun downloadStatementPdf(cardId: String, statementId: String): PdfDownloadResponse {
        val response: HttpResponse = authClient.get("$base/$cardId/statements/$statementId/pdf")
        val contentDisposition = response.headers[HttpHeaders.ContentDisposition]
        return PdfDownloadResponse(
            fileName = contentDisposition.fileNameFromDisposition() ?: "fatura-$statementId.pdf",
            contentType = response.headers[HttpHeaders.ContentType] ?: "application/pdf",
            bytes = response.body(),
        )
    }

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

data class PdfDownloadResponse(
    val fileName: String,
    val contentType: String,
    val bytes: ByteArray,
)
