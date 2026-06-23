package com.osiris.mobile.voice

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull
import kotlin.test.assertTrue

class VoiceClientTest {

    @Test
    fun ws_url_upgrades_https_to_wss() {
        assertEquals(
            "wss://osiris-api.mateussalgueiro.com.br/api/v1/ai/voice",
            voiceWsUrl("https://osiris-api.mateussalgueiro.com.br/"),
        )
    }

    @Test
    fun ws_url_upgrades_http_to_ws_without_trailing_slash() {
        assertEquals(
            "ws://10.0.2.2:13455/api/v1/ai/voice",
            voiceWsUrl("http://10.0.2.2:13455"),
        )
    }

    @Test
    fun parses_assistant_transcript() {
        val event = parseVoiceEvent("""{"type":"transcript","role":"assistant","text":"Olá","final":false}""")
        assertEquals(VoiceEvent.Transcript("assistant", "Olá", final = false), event)
    }

    @Test
    fun parses_user_transcript_with_final_flag() {
        val event = parseVoiceEvent("""{"type":"transcript","role":"user","text":"oi","final":true}""")
        assertEquals(VoiceEvent.Transcript("user", "oi", final = true), event)
    }

    @Test
    fun parses_status_and_error() {
        assertEquals(VoiceEvent.Status("interrupted"), parseVoiceEvent("""{"type":"status","value":"interrupted"}"""))
        assertEquals(VoiceEvent.Error("boom"), parseVoiceEvent("""{"type":"error","message":"boom"}"""))
    }

    @Test
    fun maps_sources_to_singleton_event() {
        assertTrue(parseVoiceEvent("""{"type":"sources","items":[]}""") is VoiceEvent.Sources)
    }

    @Test
    fun ignores_unknown_or_malformed_frames() {
        assertNull(parseVoiceEvent("""{"type":"setupComplete"}"""))
        assertNull(parseVoiceEvent("not json"))
    }
}
