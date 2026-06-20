package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import com.osiris.mobile.domain.model.CsvAmountMode
import com.osiris.mobile.domain.model.CsvImportMapping
import com.osiris.mobile.domain.model.MovementType
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.request.HttpRequestData
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.content.TextContent
import io.ktor.http.headersOf
import io.ktor.serialization.kotlinx.json.json
import kotlinx.coroutines.test.runTest
import kotlin.io.encoding.Base64
import kotlin.io.encoding.ExperimentalEncodingApi
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

class CsvImportApiTest {

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
    fun analyzeCsvImport_uploads_multipart_to_analyze_endpoint() = runTest {
        var captured: HttpRequestData? = null
        val analysisJson = """
            {"accountId":"1","accountName":"Banco","delimiter":";","encoding":"utf-8","suggestedHeaderLineIndex":0,
             "sampleRows":[["Data","Histórico","Valor"],["19/06/2026","Pix enviado","-39,99"]],
             "savedMapping":null}
        """.trimIndent()
        val (repo, _) = repository(analysisJson) { captured = it }

        val result = repo.analyzeCsvImport("1", "extrato.csv", "Data;Histórico;Valor".encodeToByteArray())

        assertTrue(result is OsirisResult.Success)
        val analysis = (result as OsirisResult.Success).value
        assertEquals(";", analysis.delimiter)
        assertEquals(2, analysis.sampleRows.size)
        assertEquals(listOf("Data", "Histórico", "Valor"), analysis.sampleRows.first())

        val request = assertNotNull(captured)
        assertTrue(request.url.encodedPath.endsWith("/accounts/1/movements/import/csv/analyze"))
        assertEquals("multipart", request.body.contentType?.contentType)
        assertEquals("form-data", request.body.contentType?.contentSubtype)
        // No delimiter/encoding sent on the first call: backend auto-detects.
        assertTrue(request.url.parameters["delimiter"] == null)
        assertTrue(request.url.parameters["encoding"] == null)
    }

    @Test
    fun analyzeCsvImport_appends_delimiter_and_encoding_query_params_when_provided() = runTest {
        var captured: HttpRequestData? = null
        val analysisJson = """
            {"accountId":"1","accountName":"Banco","delimiter":",","encoding":"windows-1252","suggestedHeaderLineIndex":0,
             "sampleRows":[["Data","Valor"]],"savedMapping":null}
        """.trimIndent()
        val (repo, _) = repository(analysisJson) { captured = it }

        val result = repo.analyzeCsvImport("1", "extrato.csv", "Data,Valor".encodeToByteArray(), ",", "windows-1252")

        assertTrue(result is OsirisResult.Success)
        val request = assertNotNull(captured)
        assertTrue(request.url.encodedPath.endsWith("/accounts/1/movements/import/csv/analyze"))
        assertEquals(",", request.url.parameters["delimiter"])
        assertEquals("windows-1252", request.url.parameters["encoding"])
        assertEquals("multipart", request.body.contentType?.contentType)
    }

    @OptIn(ExperimentalEncodingApi::class)
    @Test
    fun previewCsvImport_posts_base64_and_mapping_to_preview_endpoint() = runTest {
        var captured: HttpRequestData? = null
        val previewJson = """
            {"accountId":"1","accountName":"Banco","totalCount":1,"newCount":1,"duplicateCount":0,
             "lines":[{"rowKey":"0","externalId":"A1","occurredOn":"2026-06-19","amount":39.99,"type":2,"isInflow":false,"description":"Pix enviado","isDuplicate":false}]}
        """.trimIndent()
        val (repo, _) = repository(previewJson) { captured = it }

        val bytes = "Data;Histórico;Valor".encodeToByteArray()
        val mapping = CsvImportMapping(amountMode = CsvAmountMode.SignedAmount, amountColumn = 2)
        val result = repo.previewCsvImport("1", "extrato.csv", bytes, mapping)

        assertTrue(result is OsirisResult.Success)
        val preview = (result as OsirisResult.Success).value
        assertEquals(1, preview.totalCount)
        assertEquals(MovementType.Expense, preview.lines.single().type)

        val request = assertNotNull(captured)
        assertTrue(request.url.encodedPath.endsWith("/accounts/1/movements/import/csv/preview"))

        val body = (request.body as TextContent).text
        assertTrue(body.contains("\"fileName\":\"extrato.csv\""))
        assertTrue(body.contains("\"content\":\"${Base64.encode(bytes)}\""))
        assertTrue(body.contains("\"mapping\""))
        assertTrue(body.contains("\"amountColumn\":2"))
    }
}
