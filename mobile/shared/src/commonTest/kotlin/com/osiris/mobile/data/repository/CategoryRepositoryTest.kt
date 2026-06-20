package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.CategoryApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.sync.RecordingDataChangeBus
import com.osiris.mobile.domain.model.CategoryType
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

class CategoryRepositoryTest {

    private fun repository(
        content: String,
        status: HttpStatusCode,
        bus: RecordingDataChangeBus = RecordingDataChangeBus(),
    ): CategoryRepositoryImpl {
        val engine = MockEngine {
            respond(content, status, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        val client = HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
        return CategoryRepositoryImpl(CategoryApi(client, ApiConfig("http://test/")), bus)
    }

    @Test
    fun list_maps_dtos_to_domain() = runTest {
        val repo = repository(
            content = """[
                {"id":"1","name":"Aluguel","type":2,"color":"#F59E0B","isActive":true},
                {"id":"2","name":"Salário","type":1,"color":null,"isActive":false}
            ]""".trimIndent(),
            status = HttpStatusCode.OK,
        )

        val result = repo.list()

        assertTrue(result is OsirisResult.Success)
        val categories = (result as OsirisResult.Success).value
        assertEquals(2, categories.size)
        assertEquals(CategoryType.Expense, categories[0].type)
        assertEquals(CategoryType.Income, categories[1].type)
        assertEquals(false, categories[1].isActive)
    }

    @Test
    fun create_validationError_maps_to_field_errors() = runTest {
        val repo = repository(
            content = """{"title":"Há erros de validação.","status":400,"errors":{"Name":["Informe o nome da categoria."]}}""",
            status = HttpStatusCode.BadRequest,
        )

        val result = repo.create(name = "", type = CategoryType.Expense, color = null)

        assertTrue(result is OsirisResult.Failure)
        val error = (result as OsirisResult.Failure).error
        assertEquals("Informe o nome da categoria.", error.fieldErrors["name"])
    }

    @Test
    fun delete_inUseConflict_maps_detail_message() = runTest {
        val repo = repository(
            content = """{"title":"Não é possível excluir uma categoria em uso.","detail":"Não é possível excluir uma categoria em uso.","status":409}""",
            status = HttpStatusCode.Conflict,
        )

        val result = repo.delete("1")

        assertTrue(result is OsirisResult.Failure)
        assertEquals("Não é possível excluir uma categoria em uso.", (result as OsirisResult.Failure).error.message)
    }
}
