package com.osiris.mobile.presentation.assistant

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.domain.model.AiProposal
import com.osiris.mobile.voice.VoiceAudioCapture
import com.osiris.mobile.voice.VoiceAudioPlayback
import com.osiris.mobile.voice.VoiceClient
import com.osiris.mobile.voice.VoiceConnection
import com.osiris.mobile.voice.VoiceEvent
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class VoiceUiState(
    val active: Boolean = false,
    val connecting: Boolean = false,
    val muted: Boolean = false,
    val status: String = "idle",
    val conversationId: String? = null,
    val userText: String = "",
    val assistantText: String = "",
    val proposals: List<AiProposal> = emptyList(),
    val error: String? = null,
)

/**
 * Drives a realtime voice session: connects the [VoiceClient], streams microphone PCM16 up (gated by the
 * mute flag), plays the assistant's audio down, and projects transcripts/status into [state]. Mirrors the
 * web widget — continuous streaming with server-side VAD, barge-in flushes playback, "Encerrar" closes
 * the session. Reads are enabled but writes are not (the relay builds the context with WritesEnabled:false).
 */
class VoiceViewModel(
    private val voiceClient: VoiceClient,
    private val capture: VoiceAudioCapture,
    private val playback: VoiceAudioPlayback,
) : ViewModel() {

    private val _state = MutableStateFlow(VoiceUiState())
    val state: StateFlow<VoiceUiState> = _state.asStateFlow()

    private var sessionJob: Job? = null
    private var lastRole: String? = null

    fun start(conversationId: String? = null) {
        if (sessionJob?.isActive == true) return
        lastRole = null
        _state.value = VoiceUiState(active = true, connecting = true, conversationId = conversationId)

        sessionJob = viewModelScope.launch {
            // Drop-oldest so a slow uplink can never build latency: stale mic frames are worthless.
            val audioOut = Channel<ByteArray>(capacity = 64, onBufferOverflow = BufferOverflow.DROP_OLDEST)
            var connection: VoiceConnection? = null
            try {
                val conn = voiceClient.connect(conversationId)
                connection = conn
                playback.start()

                val sendJob = launch(Dispatchers.Default) {
                    for (chunk in audioOut) {
                        if (!_state.value.muted) {
                            runCatching { conn.sendAudio(chunk) }
                        }
                    }
                }

                capture.start { chunk -> audioOut.trySend(chunk) }
                _state.update { it.copy(connecting = false, status = "listening") }

                conn.incoming.collect { event -> handleEvent(event) }
                sendJob.cancel()
            } catch (cancellation: CancellationException) {
                throw cancellation
            } catch (throwable: Throwable) {
                _state.update { it.copy(error = "Não foi possível iniciar a voz.") }
            } finally {
                runCatching { capture.stop() }
                runCatching { playback.stop() }
                audioOut.close()
                runCatching { connection?.close() }
                _state.update { it.copy(active = false, connecting = false, status = "idle") }
            }
        }
    }

    fun toggleMute() {
        _state.update { it.copy(muted = !it.muted) }
    }

    fun stop() {
        sessionJob?.cancel()
        sessionJob = null
    }

    fun clearError() {
        _state.update { it.copy(error = null) }
    }

    fun removeProposal(proposalId: String) {
        _state.update { it.copy(proposals = it.proposals.filterNot { proposal -> proposal.id == proposalId }) }
    }

    override fun onCleared() {
        stop()
        super.onCleared()
    }

    private fun handleEvent(event: VoiceEvent) {
        when (event) {
            is VoiceEvent.Audio -> {
                playback.write(event.pcm24)
                if (_state.value.status != "speaking") {
                    _state.update { it.copy(status = "speaking") }
                }
            }

            is VoiceEvent.Transcript -> applyTranscript(event)

            is VoiceEvent.Status -> when (event.value) {
                "interrupted" -> {
                    playback.flush()
                    _state.update { it.copy(status = "listening") }
                }

                "idle" -> _state.update { it.copy(status = "listening") }
                "goingaway", "reconnecting" -> _state.update { it.copy(status = event.value) }
                "closed" -> stop()
                else -> {}
            }

            is VoiceEvent.Session -> _state.update {
                it.copy(conversationId = event.conversationId.ifBlank { it.conversationId })
            }

            is VoiceEvent.Proposal -> _state.update {
                it.copy(proposals = it.proposals + event.proposal)
            }

            is VoiceEvent.Error -> _state.update { it.copy(error = event.message) }
            VoiceEvent.Sources -> {}
            VoiceEvent.Closed -> {}
        }
    }

    private fun applyTranscript(transcript: VoiceEvent.Transcript) {
        val text = transcript.text
        if (text.isBlank()) return

        if (transcript.role == "user") {
            val newExchange = lastRole == "assistant"
            lastRole = "user"
            _state.update {
                if (newExchange) it.copy(userText = text, assistantText = "")
                else it.copy(userText = it.userText + text)
            }
        } else {
            lastRole = "assistant"
            _state.update { it.copy(assistantText = it.assistantText + text) }
        }
    }
}
