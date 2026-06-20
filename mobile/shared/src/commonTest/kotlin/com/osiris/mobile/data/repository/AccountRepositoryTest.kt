package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import com.osiris.mobile.domain.model.AccountType
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

class AccountRepositoryTest {

    private fun repository(
        content: String,
        status: HttpStatusCode,
        bus: RecordingDataChangeBus = RecordingDataChangeBus(),
    ): AccountRepositoryImpl {
        val engine = MockEngine {
            respond(content, status, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        val client = HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
        return AccountRepositoryImpl(AccountApi(client, ApiConfig("http://test/")), bus)
    }

    @Test
    fun list_maps_dtos_to_domain() = runTest {
        val repo = repository(
            content = """[{"id":"1","name":"Banco","type":1,"currentBalance":200.0,"isActive":true}]""",
            status = HttpStatusCode.OK,
        )

        val result = repo.list()

        assertTrue(result is OsirisResult.Success)
        val account = (result as OsirisResult.Success).value.single()
        assertEquals(AccountType.Checking, account.type)
        assertEquals(200.0, account.currentBalance)
    }

    @Test
    fun statement_maps_movements_with_inflow() = runTest {
        val repo = repository(
            content = """{"id":"1","name":"Caixa","type":3,"initialBalance":100.0,"currentBalance":150.0,"isActive":true,
                "movements":[{"id":"m1","type":1,"amount":50.0,"isInflow":true,"occurredOn":"2026-06-16","description":"Depósito","categoryId":null,"notes":null}]}""".trimIndent(),
            status = HttpStatusCode.OK,
        )

        val result = repo.statement("1")

        assertTrue(result is OsirisResult.Success)
        val statement = (result as OsirisResult.Success).value
        assertEquals(150.0, statement.currentBalance)
        val movement = statement.movements.single()
        assertEquals(MovementType.Income, movement.type)
        assertTrue(movement.isInflow)
    }

    @Test
    fun createMovement_validationError_maps_to_field_errors() = runTest {
        val repo = repository(
            content = """{"title":"Há erros de validação.","status":400,"errors":{"Amount":["Informe o valor do lançamento."]}}""",
            status = HttpStatusCode.BadRequest,
        )

        val result = repo.createMovement("1", MovementType.Income, 0.0, "2026-06-16", "x", null, null)

        assertTrue(result is OsirisResult.Failure)
        assertEquals("Informe o valor do lançamento.", (result as OsirisResult.Failure).error.fieldErrors["amount"])
    }
}
