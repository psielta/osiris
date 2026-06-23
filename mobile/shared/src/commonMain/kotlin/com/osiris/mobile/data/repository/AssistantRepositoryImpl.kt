package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.AiConversationDetailDto
import com.osiris.mobile.data.dto.AiConversationListItemDto
import com.osiris.mobile.data.dto.AiMessageDto
import com.osiris.mobile.data.dto.AiProposalDto
import com.osiris.mobile.data.dto.AiSourceDto
import com.osiris.mobile.data.dto.AiTurnDto
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.AssistantApi
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.domain.model.AiActionOutcome
import com.osiris.mobile.domain.model.AiConversation
import com.osiris.mobile.domain.model.AiConversationSummary
import com.osiris.mobile.domain.model.AiMessage
import com.osiris.mobile.domain.model.AiProposal
import com.osiris.mobile.domain.model.AiSource
import com.osiris.mobile.domain.model.AiTurn
import com.osiris.mobile.domain.repository.AssistantRepository

class AssistantRepositoryImpl(
    private val api: AssistantApi,
    private val bus: DataChangeBus,
) : AssistantRepository {

    override suspend fun listConversations(): OsirisResult<List<AiConversationSummary>> = osirisCatching {
        api.listConversations().map { it.toDomain() }
    }

    override suspend fun getConversation(id: String): OsirisResult<AiConversation> = osirisCatching {
        api.getConversation(id).toDomain()
    }

    override suspend fun startConversation(message: String): OsirisResult<AiTurn> = osirisCatching {
        val turn = api.startConversation(message).toDomain()
        bus.notify(DataScope.Assistant)
        turn
    }

    override suspend fun sendMessage(conversationId: String, message: String): OsirisResult<AiTurn> = osirisCatching {
        val turn = api.sendMessage(conversationId, message).toDomain()
        bus.notify(DataScope.Assistant)
        turn
    }

    override suspend fun archiveConversation(id: String): OsirisResult<Unit> = osirisCatching {
        api.archiveConversation(id)
        bus.notify(DataScope.Assistant)
    }

    override suspend fun confirmAction(proposalId: String): OsirisResult<AiActionOutcome> = osirisCatching {
        val outcome = api.confirmAction(proposalId)
        bus.notify(DataScope.Accounts, DataScope.Dashboard)
        AiActionOutcome(outcome.proposalId, outcome.status)
    }

    override suspend fun rejectAction(proposalId: String): OsirisResult<Unit> = osirisCatching {
        api.rejectAction(proposalId)
    }
}

private fun AiConversationListItemDto.toDomain() = AiConversationSummary(id, title, status)

private fun AiConversationDetailDto.toDomain() =
    AiConversation(id, title, status, messages.map { it.toDomain() })

private fun AiMessageDto.toDomain() = AiMessage(id, role, content)

private fun AiSourceDto.toDomain() = AiSource(type, id, label)

private fun AiProposalDto.toDomain() =
    AiProposal(id, actionType, displaySummary, impactSummary, riskLevel, status)

private fun AiTurnDto.toDomain() = AiTurn(
    conversationId,
    message.toDomain(),
    sources.map { it.toDomain() },
    proposals.map { it.toDomain() },
)
