package com.osiris.mobile.voice

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.remote.osirisJson
import com.osiris.mobile.data.session.SessionManager
import io.ktor.client.HttpClient
import io.ktor.client.plugins.websocket.DefaultClientWebSocketSession
import io.ktor.client.plugins.websocket.webSocketSession
import io.ktor.client.request.header
import io.ktor.client.request.url
import io.ktor.http.HttpHeaders
import io.ktor.websocket.Frame
import io.ktor.websocket.close
import io.ktor.websocket.readBytes
import io.ktor.websocket.readText
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

/**
 * Opens the JWT-authenticated WebSocket to the API voice relay (`/api/v1/ai/voice`). The backend keeps
 * the Gemini key server-side; this client only streams PCM16 audio up and receives audio + control JSON
 * down. The access token is attached as an Authorization header on the handshake; if the first attempt
 * fails (e.g. an expired token), it refreshes once and retries.
 */
class VoiceClient(
    private val wsClient: HttpClient,
    private val config: ApiConfig,
    private val session: SessionManager,
) {
    suspend fun connect(): VoiceConnection {
        val token = session.currentBundle()?.accessToken
            ?: throw IllegalStateException("Sessão expirada.")
        return try {
            open(token)
        } catch (first: Throwable) {
            val refreshed = session.refresh(null)?.accessToken ?: throw first
            open(refreshed)
        }
    }

    private suspend fun open(token: String): VoiceConnection {
        val ws = wsClient.webSocketSession {
            url(voiceWsUrl(config.baseUrl))
            header(HttpHeaders.Authorization, "Bearer $token")
        }
        return VoiceConnection(ws)
    }
}

/** Derives the `ws(s)://…/api/v1/ai/voice` endpoint from the configured `http(s)://` API base URL. */
internal fun voiceWsUrl(baseUrl: String): String {
    val trimmed = baseUrl.trimEnd('/')
    val scheme = when {
        trimmed.startsWith("https://") -> "wss://" + trimmed.removePrefix("https://")
        trimmed.startsWith("http://") -> "ws://" + trimmed.removePrefix("http://")
        else -> trimmed
    }
    return "$scheme/api/v1/ai/voice"
}

/** Parses one control-JSON frame from the relay into a [VoiceEvent]; returns null for unknown/invalid frames. */
internal fun parseVoiceEvent(text: String): VoiceEvent? {
    val obj = runCatching { osirisJson.parseToJsonElement(text).jsonObject }.getOrNull() ?: return null
    return when (obj["type"]?.jsonPrimitive?.content) {
        "transcript" -> VoiceEvent.Transcript(
            role = obj["role"]?.jsonPrimitive?.content ?: "assistant",
            text = obj["text"]?.jsonPrimitive?.content ?: "",
            final = obj["final"]?.jsonPrimitive?.booleanOrNull ?: false,
        )

        "status" -> VoiceEvent.Status(obj["value"]?.jsonPrimitive?.content ?: "")
        "error" -> VoiceEvent.Error(obj["message"]?.jsonPrimitive?.content ?: "Erro na voz.")
        "sources" -> VoiceEvent.Sources
        else -> null
    }
}

/** A live voice session. [incoming] streams server events; audio is pushed up via [sendAudio]. */
class VoiceConnection internal constructor(
    private val socket: DefaultClientWebSocketSession,
) {
    val incoming: Flow<VoiceEvent> = flow {
        for (frame in socket.incoming) {
            when (frame) {
                is Frame.Binary -> emit(VoiceEvent.Audio(frame.readBytes()))
                is Frame.Text -> parseVoiceEvent(frame.readText())?.let { emit(it) }
                else -> {}
            }
        }
        emit(VoiceEvent.Closed)
    }

    suspend fun sendAudio(bytes: ByteArray) {
        socket.send(Frame.Binary(true, bytes))
    }

    suspend fun close() {
        runCatching { socket.close() }
    }
}
