package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class SendAiMessageRequest(val message: String)

@Serializable
data class AiConversationListItemDto(
    val id: String,
    val title: String,
    val status: String,
)

@Serializable
data class AiMessageDto(
    val id: String,
    val role: String,
    val content: String,
)

@Serializable
data class AiConversationDetailDto(
    val id: String,
    val title: String,
    val status: String,
    val messages: List<AiMessageDto> = emptyList(),
)

@Serializable
data class AiSourceDto(
    val type: String,
    val id: String? = null,
    val label: String,
)

@Serializable
data class AiProposalDto(
    val id: String,
    val actionType: String,
    val displaySummary: String,
    val impactSummary: String,
    val riskLevel: String,
    val status: String,
)

@Serializable
data class AiTurnDto(
    val conversationId: String,
    val message: AiMessageDto,
    val sources: List<AiSourceDto> = emptyList(),
    val proposals: List<AiProposalDto> = emptyList(),
    val usageLimited: Boolean = false,
)

@Serializable
data class AiActionResultDto(
    val proposalId: String,
    val status: String,
    val resultEntityType: String? = null,
    val resultEntityId: String? = null,
)
