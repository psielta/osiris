package com.osiris.mobile.data.repository

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.remote.AssistantApi
import com.osiris.mobile.data.remote.osirisJson
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

class AssistantRepositoryTest {

    private fun repository(
        content: String,
        status: HttpStatusCode = HttpStatusCode.OK,
    ): AssistantRepositoryImpl {
        val engine = MockEngine {
            respond(content, status, headersOf(HttpHeaders.ContentType, "application/json"))
        }
        val client = HttpClient(engine) {
            expectSuccess = true
            install(ContentNegotiation) { json(osirisJson) }
        }
        return AssistantRepositoryImpl(AssistantApi(client, ApiConfig("http://test/")), RecordingDataChangeBus())
    }

    @Test
    fun list_conversations_maps_to_domain() = runTest {
        val repo = repository("""[{"id":"c1","title":"Junho","status":"Active"}]""")

        val result = repo.listConversations()

        assertTrue(result is OsirisResult.Success)
        val conversations = (result as OsirisResult.Success).value
        assertEquals(1, conversations.size)
        assertEquals("Junho", conversations.single().title)
    }

    @Test
    fun start_conversation_maps_turn_with_proposals() = runTest {
        val repo = repository(
            """{"conversationId":"c1","message":{"id":"m1","role":"assistant","content":"Olá"},
                "sources":[],"proposals":[{"id":"p1","actionType":"manual_movement",
                "displaySummary":"Registrar saída","impactSummary":"Impacto","riskLevel":"WriteProposal",
                "status":"Pending"}],"usageLimited":false}""".trimIndent(),
        )

        val result = repo.startConversation("registre uma saída")

        assertTrue(result is OsirisResult.Success)
        val turn = (result as OsirisResult.Success).value
        assertEquals("c1", turn.conversationId)
        assertEquals("Olá", turn.message.content)
        assertEquals(1, turn.proposals.size)
        assertEquals("Registrar saída", turn.proposals.single().displaySummary)
    }

    @Test
    fun confirm_action_maps_outcome() = runTest {
        val repo = repository(
            """{"proposalId":"p1","status":"Executed","resultEntityType":"FinancialAccountMovement","resultEntityId":"m9"}""",
        )

        val result = repo.confirmAction("p1")

        assertTrue(result is OsirisResult.Success)
        assertEquals("Executed", (result as OsirisResult.Success).value.status)
    }

    @Test
    fun conflict_maps_to_failure_message() = runTest {
        val repo = repository(
            """{"title":"Esta proposta já foi resolvida.","detail":"Esta proposta já foi resolvida.","status":409}""",
            status = HttpStatusCode.Conflict,
        )

        val result = repo.confirmAction("p1")

        assertTrue(result is OsirisResult.Failure)
        assertEquals("Esta proposta já foi resolvida.", (result as OsirisResult.Failure).error.message)
    }
}
