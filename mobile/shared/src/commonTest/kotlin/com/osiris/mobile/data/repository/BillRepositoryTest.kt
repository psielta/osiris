package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.BillApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import io.ktor.serialization.kotlinx.json.json
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

class BillRepositoryTest {
    private fun repository(
        content: String,
        status: HttpStatusCode,
        bus: RecordingDataChangeBus = RecordingDataChangeBus(),
    ): BillRepositoryImpl {
        val engine = MockEngine {
            respond(content, status, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        val client = HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
        return BillRepositoryImpl(BillApi(client, ApiConfig("http://test/")), bus)
    }

    @Test
    fun pay_emits_bill_and_account_scopes_on_success() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = repository(
            content = "",
            status = HttpStatusCode.NoContent,
            bus = bus,
        )

        val result = repo.pay("bill-1", "2026-06-16", "account-1")

        assertTrue(result is OsirisResult.Success)
        assertEquals(
            listOf(DataScope.Bills, DataScope.Accounts, DataScope.Dashboard, DataScope.Reports),
            bus.emitted,
        )
    }

    @Test
    fun pay_failure_does_not_emit() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = repository(
            content = """{"title":"Conta nao encontrada.","status":404}""",
            status = HttpStatusCode.NotFound,
            bus = bus,
        )

        val result = repo.pay("bill-1", "2026-06-16", "account-1")

        assertTrue(result is OsirisResult.Failure)
        assertTrue(bus.emitted.isEmpty())
    }
}
