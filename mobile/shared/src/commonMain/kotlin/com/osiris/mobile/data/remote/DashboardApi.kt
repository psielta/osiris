package com.osiris.mobile.data.remote

import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.dto.DashboardSummaryDto
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.parameter

class DashboardApi(
    private val authClient: HttpClient,
    config: ApiConfig,
) {
    private val base = "${config.baseUrl}api/v1/dashboard"

    suspend fun get(month: Int, year: Int): DashboardSummaryDto =
        authClient.get(base) {
            parameter("month", month)
            parameter("year", year)
        }.body()
}
