package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.statement.HttpResponse
import io.ktor.http.HttpHeaders

class ReportApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/reports"

    suspend fun downloadCashFlowSyntheticPdf(month: Int, year: Int): PdfDownloadResponse =
        downloadCashFlowPdf("$base/cash-flow/synthetic/pdf", month, year, "visao-caixa-sintetica-$year-${month.toString().padStart(2, '0')}.pdf")

    suspend fun downloadCashFlowAnalyticPdf(month: Int, year: Int): PdfDownloadResponse =
        downloadCashFlowPdf("$base/cash-flow/analytic/pdf", month, year, "visao-caixa-analitica-$year-${month.toString().padStart(2, '0')}.pdf")

    private suspend fun downloadCashFlowPdf(
        url: String,
        month: Int,
        year: Int,
        fallbackFileName: String,
    ): PdfDownloadResponse {
        val response: HttpResponse = authClient.get(url) {
            parameter("month", month)
            parameter("year", year)
        }
        val contentDisposition = response.headers[HttpHeaders.ContentDisposition]
        return PdfDownloadResponse(
            fileName = contentDisposition.fileNameFromDisposition() ?: fallbackFileName,
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
