package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.ReportApi
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

class ReportRepositoryTest {

    @Test
    fun downloadCashFlowSyntheticPdf_uses_report_endpoint_and_maps_pdf() = runTest {
        val requestedUrls = mutableListOf<String>()
        val repo = repository(requestedUrls, "visao-caixa-sintetica-2026-06.pdf")

        val result = repo.downloadCashFlowSyntheticPdf(month = 6, year = 2026)

        assertTrue(result is OsirisResult.Success)
        val pdf = (result as OsirisResult.Success).value
        assertEquals("visao-caixa-sintetica-2026-06.pdf", pdf.fileName)
        assertEquals("application/pdf", pdf.contentType)
        assertEquals("%PDF", pdf.bytes.decodeToString(0, 4))
        assertEquals(
            "http://test/api/v1/reports/cash-flow/synthetic/pdf?month=6&year=2026",
            requestedUrls.single(),
        )
    }

    @Test
    fun downloadCashFlowAnalyticPdf_uses_report_endpoint_and_maps_pdf() = runTest {
        val requestedUrls = mutableListOf<String>()
        val repo = repository(requestedUrls, "visao-caixa-analitica-2026-06.pdf")

        val result = repo.downloadCashFlowAnalyticPdf(month = 6, year = 2026)

        assertTrue(result is OsirisResult.Success)
        val pdf = (result as OsirisResult.Success).value
        assertEquals("visao-caixa-analitica-2026-06.pdf", pdf.fileName)
        assertEquals("application/pdf", pdf.contentType)
        assertEquals("%PDF", pdf.bytes.decodeToString(0, 4))
        assertEquals(
            "http://test/api/v1/reports/cash-flow/analytic/pdf?month=6&year=2026",
            requestedUrls.single(),
        )
    }

    private fun repository(requestedUrls: MutableList<String>, fileName: String): ReportRepositoryImpl {
        val engine = MockEngine { request ->
            requestedUrls += request.url.toString()
            respond(
                content = "%PDF test",
                status = HttpStatusCode.OK,
                headers = headersOf(
                    HttpHeaders.ContentType to listOf("application/pdf"),
                    HttpHeaders.ContentDisposition to listOf("attachment; filename=\"$fileName\""),
                ),
            )
        }
        val client = HttpClient(engine) {
            expectSuccess = true
        }
        return ReportRepositoryImpl(ReportApi(client, ApiConfig("http://test/")))
    }
}
