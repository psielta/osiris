package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.StatementPdf

interface ReportRepository {
    suspend fun downloadCashFlowSyntheticPdf(month: Int, year: Int): OsirisResult<StatementPdf>

    suspend fun downloadCashFlowAnalyticPdf(month: Int, year: Int): OsirisResult<StatementPdf>
}
