package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.domain.model.OfxImportSelection
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
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

class OfxImportApiTest {

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
    fun previewOfxImport_uploads_multipart_to_preview_endpoint() = runTest {
        var captured: HttpRequestData? = null
        val previewJson = """
            {"accountId":"1","accountName":"Banco","totalCount":1,"newCount":1,"duplicateCount":0,
             "lines":[{"rowKey":"0","externalId":"A1","occurredOn":"2026-06-02","amount":1500.0,"type":1,"isInflow":true,"description":"Salario","isDuplicate":false}]}
        """.trimIndent()
        val (repo, _) = repository(previewJson) { captured = it }

        val result = repo.previewOfxImport("1", "extrato.ofx", "<OFX></OFX>".encodeToByteArray())

        assertTrue(result is OsirisResult.Success)
        val preview = (result as OsirisResult.Success).value
        assertEquals(1, preview.totalCount)
        assertEquals(MovementType.Income, preview.lines.single().type)

        val request = assertNotNull(captured)
        assertTrue(request.url.encodedPath.endsWith("/accounts/1/movements/import/preview"))
        assertEquals("multipart", request.body.contentType?.contentType)
        assertEquals("form-data", request.body.contentType?.contentSubtype)
    }

    @Test
    fun confirmOfxImport_posts_positive_amount_and_type_then_notifies() = runTest {
        var captured: HttpRequestData? = null
        val (repo, bus) = repository("""{"imported":1,"skippedDuplicates":0,"total":1}""") { captured = it }

        val selections = listOf(
            OfxImportSelection("A1", "2026-06-02", 1500.0, MovementType.Income, "Salario", null),
        )
        val result = repo.confirmOfxImport("1", selections)

        assertTrue(result is OsirisResult.Success)
        val request = assertNotNull(captured)
        assertTrue(request.url.encodedPath.endsWith("/accounts/1/movements/import"))

        val body = (request.body as TextContent).text
        assertTrue(body.contains("\"externalId\":\"A1\""))
        assertTrue(body.contains("\"amount\":1500"))
        assertTrue(body.contains("\"type\":1"))

        assertTrue(bus.emitted.contains(DataScope.Accounts))
    }

    @Test
    fun previewOfxImport_parses_reconciliation_suggestions() = runTest {
        val previewJson = """
            {"accountId":"1","accountName":"Banco","totalCount":1,"newCount":1,"duplicateCount":0,"suggestedReconciliationCount":1,
             "lines":[{"rowKey":"0","externalId":"A1","occurredOn":"2026-06-02","amount":1500.0,"type":1,"isInflow":true,"description":"Salario","isDuplicate":false,
              "suggestedMovementId":"mov-1","candidates":[{"movementId":"mov-1","occurredOn":"2026-06-02","amount":1500.0,"isInflow":true,"description":"Salario manual","score":0.9,"isConfident":true}]}]}
        """.trimIndent()
        val (repo, _) = repository(previewJson) { }

        val result = repo.previewOfxImport("1", "extrato.ofx", "<OFX></OFX>".encodeToByteArray())

        assertTrue(result is OsirisResult.Success)
        val preview = (result as OsirisResult.Success).value
        assertEquals(1, preview.suggestedReconciliationCount)
        val line = preview.lines.single()
        assertEquals("mov-1", line.suggestedMovementId)
        assertEquals("mov-1", line.candidates.single().movementId)
    }

    @Test
    fun confirmOfxImport_sends_reconcile_target_and_maps_reconciled_count() = runTest {
        var captured: HttpRequestData? = null
        val (repo, _) = repository("""{"imported":0,"reconciled":1,"skippedDuplicates":0,"total":1}""") { captured = it }

        val selections = listOf(
            OfxImportSelection("A1", "2026-06-02", 100.0, MovementType.Income, "Salario", null, "mov-123"),
        )
        val result = repo.confirmOfxImport("1", selections)

        assertTrue(result is OsirisResult.Success)
        assertEquals(1, (result as OsirisResult.Success).value.reconciled)

        val body = ((assertNotNull(captured)).body as TextContent).text
        assertTrue(body.contains("\"reconcileWithMovementId\":\"mov-123\""))
    }
}
