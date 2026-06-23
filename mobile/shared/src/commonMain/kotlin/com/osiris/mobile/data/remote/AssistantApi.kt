package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.AiActionResultDto
import com.osiris.mobile.data.dto.AiConversationDetailDto
import com.osiris.mobile.data.dto.AiConversationListItemDto
import com.osiris.mobile.data.dto.AiTurnDto
import com.osiris.mobile.data.dto.SendAiMessageRequest
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.contentType

class AssistantApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/ai"

    suspend fun listConversations(): List<AiConversationListItemDto> =
        authClient.get("$base/conversations").body()

    suspend fun getConversation(id: String): AiConversationDetailDto =
        authClient.get("$base/conversations/$id").body()

    suspend fun startConversation(message: String): AiTurnDto =
        authClient.post("$base/conversations") {
            contentType(ContentType.Application.Json)
            setBody(SendAiMessageRequest(message))
        }.body()

    suspend fun sendMessage(conversationId: String, message: String): AiTurnDto =
        authClient.post("$base/conversations/$conversationId/messages") {
            contentType(ContentType.Application.Json)
            setBody(SendAiMessageRequest(message))
        }.body()

    suspend fun archiveConversation(id: String) {
        authClient.post("$base/conversations/$id/archive")
    }

    suspend fun confirmAction(proposalId: String): AiActionResultDto =
        authClient.post("$base/actions/$proposalId/confirm").body()

    suspend fun rejectAction(proposalId: String) {
        authClient.post("$base/actions/$proposalId/reject")
    }
}
