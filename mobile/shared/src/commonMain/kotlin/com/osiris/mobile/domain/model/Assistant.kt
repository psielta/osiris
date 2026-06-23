package com.osiris.mobile.domain.model

data class AiConversationSummary(
    val id: String,
    val title: String,
    val status: String,
)

data class AiMessage(
    val id: String,
    val role: String,
    val content: String,
) {
    val isUser: Boolean get() = role.equals("user", ignoreCase = true)
}

data class AiConversation(
    val id: String,
    val title: String,
    val status: String,
    val messages: List<AiMessage>,
)

data class AiSource(
    val type: String,
    val id: String?,
    val label: String,
)

data class AiProposal(
    val id: String,
    val actionType: String,
    val displaySummary: String,
    val impactSummary: String,
    val riskLevel: String,
    val status: String,
)

/** The result of one assistant turn: the assistant reply plus any sources and pending proposals. */
data class AiTurn(
    val conversationId: String,
    val message: AiMessage,
    val sources: List<AiSource>,
    val proposals: List<AiProposal>,
)

data class AiActionOutcome(
    val proposalId: String,
    val status: String,
)
