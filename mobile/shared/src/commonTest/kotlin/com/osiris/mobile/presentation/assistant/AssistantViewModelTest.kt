package com.osiris.mobile.presentation.assistant

import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisError
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DefaultDataChangeBus
import com.osiris.mobile.domain.model.AiActionOutcome
import com.osiris.mobile.domain.model.AiConversation
import com.osiris.mobile.domain.model.AiConversationSummary
import com.osiris.mobile.domain.model.AiMessage
import com.osiris.mobile.domain.model.AiProposal
import com.osiris.mobile.domain.model.AiTurn
import com.osiris.mobile.domain.repository.AssistantRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.cancel
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

@OptIn(ExperimentalCoroutinesApi::class)
class AssistantViewModelTest {

    @Test
    fun loads_conversations_on_init() = runTest {
        val dispatcher = StandardTestDispatcher(testScheduler)
        Dispatchers.setMain(dispatcher)
        val repository = FakeAssistantRepository(
            conversations = listOf(AiConversationSummary("c1", "Junho", "Active")),
        )
        val viewModel = AssistantViewModel(repository, DefaultDataChangeBus())

        try {
            advanceUntilIdle()
            assertEquals(1, viewModel.state.value.conversations.size)
            assertEquals("Junho", viewModel.state.value.conversations.single().title)
        } finally {
            viewModel.viewModelScope.cancel()
            Dispatchers.resetMain()
        }
    }

    @Test
    fun send_starts_conversation_and_surfaces_proposals_and_history() = runTest {
        val dispatcher = StandardTestDispatcher(testScheduler)
        Dispatchers.setMain(dispatcher)
        val repository = FakeAssistantRepository(
            turn = AiTurn(
                conversationId = "c1",
                message = AiMessage("m1", "assistant", "Olá"),
                sources = emptyList(),
                proposals = listOf(AiProposal("p1", "manual_movement", "Registrar saída", "Impacto", "WriteProposal", "Pending")),
            ),
            conversation = AiConversation(
                "c1",
                "Junho",
                "Active",
                listOf(AiMessage("u1", "user", "registre"), AiMessage("m1", "assistant", "Olá")),
            ),
        )
        val viewModel = AssistantViewModel(repository, DefaultDataChangeBus())

        try {
            advanceUntilIdle()
            viewModel.send("registre uma saída de 50")
            advanceUntilIdle()

            assertEquals("c1", viewModel.state.value.selectedConversationId)
            assertEquals(2, viewModel.state.value.messages.size)
            assertEquals(1, viewModel.state.value.proposals.size)
        } finally {
            viewModel.viewModelScope.cancel()
            Dispatchers.resetMain()
        }
    }

    @Test
    fun confirm_removes_the_proposal() = runTest {
        val dispatcher = StandardTestDispatcher(testScheduler)
        Dispatchers.setMain(dispatcher)
        val repository = FakeAssistantRepository(
            turn = AiTurn(
                conversationId = "c1",
                message = AiMessage("m1", "assistant", "Olá"),
                sources = emptyList(),
                proposals = listOf(AiProposal("p1", "manual_movement", "Registrar saída", "Impacto", "WriteProposal", "Pending")),
            ),
            conversation = AiConversation("c1", "Junho", "Active", emptyList()),
        )
        val viewModel = AssistantViewModel(repository, DefaultDataChangeBus())

        try {
            advanceUntilIdle()
            viewModel.send("registre")
            advanceUntilIdle()
            assertEquals(1, viewModel.state.value.proposals.size)

            viewModel.confirm("p1")
            advanceUntilIdle()

            assertTrue(viewModel.state.value.proposals.isEmpty())
            assertEquals(1, repository.confirmCalls)
        } finally {
            viewModel.viewModelScope.cancel()
            Dispatchers.resetMain()
        }
    }
}

private class FakeAssistantRepository(
    private val conversations: List<AiConversationSummary> = emptyList(),
    private val turn: AiTurn? = null,
    private val conversation: AiConversation? = null,
) : AssistantRepository {
    var confirmCalls = 0

    override suspend fun listConversations(): OsirisResult<List<AiConversationSummary>> =
        OsirisResult.Success(conversations)

    override suspend fun getConversation(id: String): OsirisResult<AiConversation> =
        conversation?.let { OsirisResult.Success(it) } ?: OsirisResult.Failure(OsirisError("Sem detalhe."))

    override suspend fun startConversation(message: String): OsirisResult<AiTurn> =
        turn?.let { OsirisResult.Success(it) } ?: OsirisResult.Failure(OsirisError("Sem turno."))

    override suspend fun sendMessage(conversationId: String, message: String): OsirisResult<AiTurn> =
        startConversation(message)

    override suspend fun archiveConversation(id: String): OsirisResult<Unit> = OsirisResult.Success(Unit)

    override suspend fun confirmAction(proposalId: String): OsirisResult<AiActionOutcome> {
        confirmCalls += 1
        return OsirisResult.Success(AiActionOutcome(proposalId, "Executed"))
    }

    override suspend fun rejectAction(proposalId: String): OsirisResult<Unit> = OsirisResult.Success(Unit)
}
