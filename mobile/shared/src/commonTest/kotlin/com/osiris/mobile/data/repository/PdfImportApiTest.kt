package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import com.osiris.mobile.domain.model.MovementType
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.request.HttpRequestData
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import io.ktor.serialization.kotlinx.json.json
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

class PdfImportApiTest {

    private fun repository(
        responseJson: String,
        capture: (HttpRequestData) -> Unit,
    ): Pair<AccountRepositoryImpl, RecordingDataChangeBus> {
        val bus = RecordingDataChangeBus()
        val engine = MockEngine { request ->
            capture(request)
            respond(responseJson, HttpStatusCode.OK, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        val client = HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
        return AccountRepositoryImpl(AccountApi(client, ApiConfig("http://test/")), bus) to bus
    }

    @Test
    fun previewPdfImport_uploads_multipart_to_pdf_preview_endpoint() = runTest {
        var captured: HttpRequestData? = null
        val previewJson = """
            {"accountId":"1","accountName":"Banco","totalCount":1,"newCount":1,"duplicateCount":0,
             "lines":[{"rowKey":"0","externalId":"A1","occurredOn":"2026-06-02","amount":1500.0,"type":1,"isInflow":true,"description":"Salario","isDuplicate":false}]}
        """.trimIndent()
        val (repo, _) = repository(previewJson) { captured = it }

        val result = repo.previewPdfImport("1", "extrato.pdf", "%PDF-1.4".encodeToByteArray())

        assertTrue(result is OsirisResult.Success)
        val preview = (result as OsirisResult.Success).value
        assertEquals(1, preview.totalCount)
        assertEquals(MovementType.Income, preview.lines.single().type)

        val request = assertNotNull(captured)
        assertTrue(request.url.encodedPath.endsWith("/accounts/1/movements/import/pdf/preview"))
        assertEquals("multipart", request.body.contentType?.contentType)
        assertEquals("form-data", request.body.contentType?.contentSubtype)
    }
}
