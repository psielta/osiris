package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.CardApi
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.domain.model.StatementStatus
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

class CardRepositoryTest {

    private fun repository(content: String, status: HttpStatusCode = HttpStatusCode.OK): CardRepositoryImpl {
        val engine = MockEngine {
            respond(content, status, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        val client = HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
        return CardRepositoryImpl(CardApi(client, ApiConfig("http://test/")))
    }

    @Test
    fun listCards_maps_dtos_to_domain() = runTest {
        val repo = repository("""[{"id":"1","name":"Nubank","limit":1500.0,"closingDay":3,"dueDay":10,"isActive":true}]""")

        val result = repo.listCards()

        assertTrue(result is OsirisResult.Success)
        val card = (result as OsirisResult.Success).value.single()
        assertEquals("Nubank", card.name)
        assertEquals(1500.0, card.limit)
        assertTrue(card.isActive)
    }

    @Test
    fun purchaseDetails_maps_installments() = runTest {
        val repo = repository(
            """{"id":"p1","creditCardId":"c1","creditCardName":"Nubank","categoryName":"Mercado","description":"Compra",
                "totalAmount":100.0,"purchaseDate":"2026-06-20","installments":2,"notes":null,
                "installmentItems":[{"id":"i1","installmentNumber":1,"totalInstallments":2,"amount":50.0,"dueDate":"2026-07-05",
                "creditCardStatementId":"s1","referenceMonth":6,"referenceYear":2026}]}""".trimIndent(),
        )

        val result = repo.getPurchase("c1", "p1")

        assertTrue(result is OsirisResult.Success)
        val purchase = (result as OsirisResult.Success).value
        assertEquals("Mercado", purchase.categoryName)
        assertEquals(1, purchase.installmentItems.single().installmentNumber)
        assertEquals(2026, purchase.installmentItems.single().referenceYear)
    }

    @Test
    fun statementDetails_maps_status_payments_and_installments() = runTest {
        val repo = repository(
            """{"id":"s1","creditCardId":"c1","creditCardName":"Nubank","referenceMonth":6,"referenceYear":2026,
                "closingDate":"2026-06-25","dueDate":"2026-07-05","status":4,"totalAmount":100.0,"paidAmount":40.0,"openBalance":60.0,
                "installmentItems":[{"id":"i1","creditCardPurchaseId":"p1","purchaseDescription":"Compra","installmentNumber":1,"totalInstallments":1,"amount":100.0}],
                "payments":[{"id":"pay1","amount":40.0,"paidAt":"2026-06-30","financialAccountId":"a1","financialAccountName":"Banco","notes":null}]}""".trimIndent(),
        )

        val result = repo.getStatement("c1", "s1")

        assertTrue(result is OsirisResult.Success)
        val statement = (result as OsirisResult.Success).value
        assertEquals(StatementStatus.PartiallyPaid, statement.status)
        assertEquals("Compra", statement.installmentItems.single().purchaseDescription)
        assertEquals("Banco", statement.payments.single().financialAccountName)
    }

    @Test
    fun preview_maps_projected_limit() = runTest {
        val repo = repository(
            """{"installments":[{"number":1,"amount":33.33,"referenceMonth":6,"referenceYear":2026,"dueDate":"2026-07-05"}],
                "totalAmount":33.33,"limit":1000.0,"usedLimit":100.0,"availableLimit":900.0,
                "projectedUsedLimit":133.33,"projectedUsagePercentage":13.3,"highLimitUsage":false,
                "projectedFutureInstallmentsTotal":0.0}""".trimIndent(),
        )

        val result = repo.previewPurchase("c1", 33.33, "2026-06-20", 1)

        assertTrue(result is OsirisResult.Success)
        val preview = (result as OsirisResult.Success).value!!
        assertEquals(133.33, preview.projectedUsedLimit)
        assertEquals(33.33, preview.installments.single().amount)
    }

    @Test
    fun createCard_validationError_maps_to_field_errors() = runTest {
        val repo = repository(
            content = """{"title":"Ha erros de validacao.","status":400,"errors":{"Limit":["Informe o limite do cartao."]}}""",
            status = HttpStatusCode.BadRequest,
        )

        val result = repo.createCard("Nubank", -1.0, 3, 10, null)

        assertTrue(result is OsirisResult.Failure)
        assertEquals("Informe o limite do cartao.", (result as OsirisResult.Failure).error.fieldErrors["limit"])
    }
}
