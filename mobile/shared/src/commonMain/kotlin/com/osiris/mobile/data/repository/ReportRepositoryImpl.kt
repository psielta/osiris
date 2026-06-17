package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.ReportApi
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.ReportRepository

class ReportRepositoryImpl(private val api: ReportApi) : ReportRepository {
    override suspend fun downloadCashFlowSyntheticPdf(month: Int, year: Int): OsirisResult<StatementPdf> =
        osirisCatching {
            val response = api.downloadCashFlowSyntheticPdf(month, year)
            StatementPdf(response.fileName, response.contentType, response.bytes)
        }

    override suspend fun downloadCashFlowAnalyticPdf(month: Int, year: Int): OsirisResult<StatementPdf> =
        osirisCatching {
            val response = api.downloadCashFlowAnalyticPdf(month, year)
            StatementPdf(response.fileName, response.contentType, response.bytes)
        }
}
