package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.BillDetailsDto
import com.osiris.mobile.data.dto.BillListItemDto
import com.osiris.mobile.data.dto.CreateBillRequest
import com.osiris.mobile.data.dto.CreatedIdResponse
import com.osiris.mobile.data.dto.PayBillRequest
import com.osiris.mobile.data.dto.UpdateBillRequest
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.delete
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.request.post
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.contentType

class BillApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/bills"

    suspend fun list(month: Int, year: Int): List<BillListItemDto> =
        authClient.get(base) {
            parameter("month", month)
            parameter("year", year)
        }.body()

    suspend fun get(id: String): BillDetailsDto =
        authClient.get("$base/$id").body()

    suspend fun create(request: CreateBillRequest): CreatedIdResponse =
        authClient.post(base) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun update(id: String, request: UpdateBillRequest) {
        authClient.put("$base/$id") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun delete(id: String) {
        authClient.delete("$base/$id")
    }

    suspend fun pay(id: String, request: PayBillRequest) {
        authClient.post("$base/$id/pay") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun markPending(id: String) {
        authClient.post("$base/$id/pending")
    }
}
