package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Bill
import com.osiris.mobile.domain.model.BillDetails

interface BillRepository {
    suspend fun list(month: Int, year: Int): OsirisResult<List<Bill>>
    suspend fun get(id: String): OsirisResult<BillDetails>
    suspend fun create(
        description: String,
        amount: Double,
        dueDate: String,
        categoryId: String,
        paymentAccountId: String?,
        notes: String?,
    ): OsirisResult<Unit>
    suspend fun update(
        id: String,
        description: String,
        amount: Double,
        dueDate: String,
        categoryId: String,
        paymentAccountId: String?,
        notes: String?,
    ): OsirisResult<Unit>
    suspend fun delete(id: String): OsirisResult<Unit>
    suspend fun pay(id: String, paidAt: String, paymentAccountId: String?): OsirisResult<Unit>
    suspend fun markPending(id: String): OsirisResult<Unit>
}
