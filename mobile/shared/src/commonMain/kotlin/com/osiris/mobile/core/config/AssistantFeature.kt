package com.osiris.mobile.core.config

/**
 * Client-side switch for the AI assistant surface on mobile (mirrors the server-side
 * `Features:AiAssistantMobile` flag). The server still gates the endpoints, so even with this on the
 * API answers 404 when the assistant is disabled server-side.
 */
object AssistantFeature {
    const val Enabled: Boolean = true
}
