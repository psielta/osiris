package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.CategoryEditDto
import com.osiris.mobile.data.dto.CategoryListItemDto
import com.osiris.mobile.data.dto.CreateCategoryRequest
import com.osiris.mobile.data.dto.CreatedCategoryResponse
import com.osiris.mobile.data.dto.UpdateCategoryRequest
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.delete
import io.ktor.client.request.get
import io.ktor.client.request.post
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.contentType

class CategoryApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/categories"

    suspend fun list(): List<CategoryListItemDto> =
        authClient.get(base).body()

    suspend fun get(id: String): CategoryEditDto =
        authClient.get("$base/$id").body()

    suspend fun create(request: CreateCategoryRequest): CreatedCategoryResponse =
        authClient.post(base) {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun update(id: String, request: UpdateCategoryRequest) {
        authClient.put("$base/$id") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun archive(id: String) {
        authClient.post("$base/$id/archive")
    }

    suspend fun delete(id: String) {
        authClient.delete("$base/$id")
    }
}
