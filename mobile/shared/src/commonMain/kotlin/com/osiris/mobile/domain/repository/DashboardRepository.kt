package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.DashboardSummary

interface DashboardRepository {
    suspend fun get(month: Int, year: Int): OsirisResult<DashboardSummary>
}
