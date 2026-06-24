package com.osiris.mobile.voice

import com.osiris.mobile.domain.model.AiProposal

/**
 * Events the voice relay forwards to the client over the WebSocket. Binary frames become [Audio];
 * the control JSON the server sends (`transcript`, `status`, `sources`, `error`) maps to the rest.
 * Mirrors the payloads produced by AiVoiceRelay on the backend.
 */
sealed interface VoiceEvent {
    /** A PCM16 mono 24 kHz chunk of the assistant's speech, ready to be played back. */
    data class Audio(val pcm24: ByteArray) : VoiceEvent {
        override fun equals(other: Any?): Boolean =
            this === other || (other is Audio && pcm24.contentEquals(other.pcm24))

        override fun hashCode(): Int = pcm24.contentHashCode()
    }

    /** Incremental speech-to-text for either side (`role` = "user" or "assistant"). */
    data class Transcript(val role: String, val text: String, val final: Boolean) : VoiceEvent

    /** Session status: "listening", "speaking", "interrupted" (barge-in), "idle", "goingaway", "closed". */
    data class Status(val value: String) : VoiceEvent

    /** The backend created or resumed a persisted assistant conversation for this voice session. */
    data class Session(val conversationId: String, val writesEnabled: Boolean) : VoiceEvent

    /** A confirmable write proposal created by a voice tool call. Confirmation still uses REST. */
    data class Proposal(val proposal: AiProposal) : VoiceEvent

    /** The relay surfaced grounding sources from a tool call; details are not shown in the voice UI yet. */
    data object Sources : VoiceEvent

    /** The relay reported a fatal error for this session. */
    data class Error(val message: String) : VoiceEvent

    /** The WebSocket closed; the read loop has ended. */
    data object Closed : VoiceEvent
}
