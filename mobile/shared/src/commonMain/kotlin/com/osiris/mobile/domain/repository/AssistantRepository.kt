package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.AiActionOutcome
import com.osiris.mobile.domain.model.AiConversation
import com.osiris.mobile.domain.model.AiConversationSummary
import com.osiris.mobile.domain.model.AiTurn

interface AssistantRepository {
    suspend fun listConversations(): OsirisResult<List<AiConversationSummary>>
    suspend fun getConversation(id: String): OsirisResult<AiConversation>
    suspend fun startConversation(message: String): OsirisResult<AiTurn>
    suspend fun sendMessage(conversationId: String, message: String): OsirisResult<AiTurn>
    suspend fun archiveConversation(id: String): OsirisResult<Unit>
    suspend fun confirmAction(proposalId: String): OsirisResult<AiActionOutcome>
    suspend fun rejectAction(proposalId: String): OsirisResult<Unit>
}
