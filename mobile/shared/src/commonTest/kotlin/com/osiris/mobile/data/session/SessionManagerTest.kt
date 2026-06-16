package com.osiris.mobile.data.session

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.remote.osirisJson
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import io.ktor.serialization.kotlinx.json.json
import kotlinx.coroutines.async
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals

class SessionManagerTest {

    private val refreshBody = """
        {"accessToken":"newAccess","refreshToken":"newRefresh","tokenType":"Bearer",
         "user":{"fullName":"Jane","email":"jane@osiris.test","tenantName":"Acme"}}
    """.trimIndent()

    private fun clientCounting(onCall: () -> Unit): HttpClient {
        val engine = MockEngine {
            onCall()
            respond(
                content = refreshBody,
                status = HttpStatusCode.OK,
                headers = headersOf(HttpHeaders.ContentType, "application/json"),
            )
        }
        return HttpClient(engine) {
            install(ContentNegotiation) { json(osirisJson) }
        }
    }

    @Test
    fun refresh_rotates_and_persists() = runTest {
        var calls = 0
        val store = FakeTokenStore(TokenBundle("oldAccess", "oldRefresh"))
        val session = SessionManager(store, clientCounting { calls++ }, ApiConfig("http://test/"))
        session.bootstrap()

        val result = session.refresh("oldRefresh")

        assertEquals(TokenBundle("newAccess", "newRefresh"), result)
        assertEquals(TokenBundle("newAccess", "newRefresh"), store.bundle)
        assertEquals(1, calls)
    }

    @Test
    fun concurrent_refresh_calls_network_once() = runTest {
        var calls = 0
        val store = FakeTokenStore(TokenBundle("oldAccess", "oldRefresh"))
        val session = SessionManager(store, clientCounting { calls++ }, ApiConfig("http://test/"))
        session.bootstrap()

        val first = async { session.refresh("oldRefresh") }
        val second = async { session.refresh("oldRefresh") }
        val a = first.await()
        val b = second.await()

        // Single-flight: the second caller reuses the rotated bundle instead of hitting the network.
        assertEquals(1, calls)
        assertEquals("newRefresh", a?.refreshToken)
        assertEquals("newRefresh", b?.refreshToken)
    }
}
