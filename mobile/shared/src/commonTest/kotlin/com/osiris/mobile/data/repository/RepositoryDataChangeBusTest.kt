package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.remote.CardApi
import com.osiris.mobile.data.remote.CategoryApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.domain.model.MovementType
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

class RepositoryDataChangeBusTest {
    @Test
    fun account_write_emits_account_dashboard_and_report_scopes_on_success() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = AccountRepositoryImpl(
            AccountApi(client("""{"id":"m1"}""", HttpStatusCode.Created), ApiConfig("http://test/")),
            bus,
        )

        val result = repo.createMovement("1", MovementType.Income, 100.0, "2026-06-16", "Receita", null, null)

        assertTrue(result is OsirisResult.Success)
        assertEquals(listOf(DataScope.Accounts, DataScope.Dashboard, DataScope.Reports), bus.emitted)
    }

    @Test
    fun account_write_does_not_emit_on_failure() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = AccountRepositoryImpl(
            AccountApi(client("""{"title":"Invalid request.","status":400}""", HttpStatusCode.BadRequest), ApiConfig("http://test/")),
            bus,
        )

        val result = repo.createMovement("1", MovementType.Income, 0.0, "2026-06-16", "x", null, null)

        assertTrue(result is OsirisResult.Failure)
        assertTrue(bus.emitted.isEmpty())
    }

    @Test
    fun category_write_emits_categories_scope_on_success() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = CategoryRepositoryImpl(
            CategoryApi(client("", HttpStatusCode.NoContent), ApiConfig("http://test/")),
            bus,
        )

        val result = repo.update("1", "Aluguel", CategoryType.Expense, "#F59E0B")

        assertTrue(result is OsirisResult.Success)
        assertEquals(listOf(DataScope.Categories), bus.emitted)
    }

    @Test
    fun category_write_does_not_emit_on_failure() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = CategoryRepositoryImpl(
            CategoryApi(client("""{"title":"Invalid request.","status":400}""", HttpStatusCode.BadRequest), ApiConfig("http://test/")),
            bus,
        )

        val result = repo.update("1", "", CategoryType.Expense, null)

        assertTrue(result is OsirisResult.Failure)
        assertTrue(bus.emitted.isEmpty())
    }

    @Test
    fun card_payment_emits_card_account_dashboard_and_report_scopes_on_success() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = CardRepositoryImpl(
            CardApi(client("""{"id":"payment-1"}""", HttpStatusCode.Created), ApiConfig("http://test/")),
            bus,
        )

        val result = repo.payStatement("card-1", "statement-1", 100.0, "2026-06-16", "account-1", null)

        assertTrue(result is OsirisResult.Success)
        assertEquals(
            listOf(DataScope.Cards, DataScope.Accounts, DataScope.Dashboard, DataScope.Reports),
            bus.emitted,
        )
    }

    @Test
    fun card_payment_does_not_emit_on_failure() = runTest {
        val bus = RecordingDataChangeBus()
        val repo = CardRepositoryImpl(
            CardApi(client("""{"title":"Invalid request.","status":400}""", HttpStatusCode.BadRequest), ApiConfig("http://test/")),
            bus,
        )

        val result = repo.payStatement("card-1", "statement-1", 0.0, "2026-06-16", "account-1", null)

        assertTrue(result is OsirisResult.Failure)
        assertTrue(bus.emitted.isEmpty())
    }

    private fun client(content: String, status: HttpStatusCode): HttpClient {
        val engine = MockEngine {
            respond(content, status, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        return HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
    }
}
