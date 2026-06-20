package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.BillDetailsDto
import com.osiris.mobile.data.dto.BillListItemDto
import com.osiris.mobile.data.dto.CreateBillRequest
import com.osiris.mobile.data.dto.PayBillRequest
import com.osiris.mobile.data.dto.UpdateBillRequest
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.BillApi
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.domain.model.Bill
import com.osiris.mobile.domain.model.BillDetails
import com.osiris.mobile.domain.model.BillStatus
import com.osiris.mobile.domain.repository.BillRepository

class BillRepositoryImpl(
    private val api: BillApi,
    private val bus: DataChangeBus,
) : BillRepository {

    override suspend fun list(month: Int, year: Int): OsirisResult<List<Bill>> = osirisCatching {
        api.list(month, year).map { it.toDomain() }
    }

    override suspend fun get(id: String): OsirisResult<BillDetails> = osirisCatching {
        api.get(id).toDomain()
    }

    override suspend fun create(
        description: String,
        amount: Double,
        dueDate: String,
        categoryId: String,
        paymentAccountId: String?,
        notes: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.create(CreateBillRequest(description, amount, dueDate, categoryId, paymentAccountId, notes))
        bus.notify(DataScope.Bills, DataScope.Dashboard, DataScope.Reports)
        Unit
    }

    override suspend fun update(
        id: String,
        description: String,
        amount: Double,
        dueDate: String,
        categoryId: String,
        paymentAccountId: String?,
        notes: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.update(id, UpdateBillRequest(description, amount, dueDate, categoryId, paymentAccountId, notes))
        bus.notify(DataScope.Bills, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun delete(id: String): OsirisResult<Unit> = osirisCatching {
        api.delete(id)
        bus.notify(DataScope.Bills, DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun pay(id: String, paidAt: String, paymentAccountId: String?): OsirisResult<Unit> =
        osirisCatching {
            api.pay(id, PayBillRequest(paidAt, paymentAccountId))
            bus.notify(DataScope.Bills, DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
        }

    override suspend fun markPending(id: String): OsirisResult<Unit> = osirisCatching {
        api.markPending(id)
        bus.notify(DataScope.Bills, DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
    }
}

private fun BillListItemDto.toDomain() =
    Bill(
        id,
        description,
        amount,
        dueDate,
        paidAt,
        BillStatus.fromApi(status),
        categoryId,
        categoryName,
        categoryColor,
        paymentAccountId,
        paymentAccountName,
    )

private fun BillDetailsDto.toDomain() =
    BillDetails(
        id,
        description,
        amount,
        dueDate,
        paidAt,
        BillStatus.fromApi(status),
        categoryId,
        categoryName,
        categoryColor,
        paymentAccountId,
        paymentAccountName,
        notes,
    )
