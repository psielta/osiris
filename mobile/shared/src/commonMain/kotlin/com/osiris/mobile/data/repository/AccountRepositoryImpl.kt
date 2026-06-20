package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.AccountEditDto
import com.osiris.mobile.data.dto.AccountListItemDto
import com.osiris.mobile.data.dto.CreateAccountRequest
import com.osiris.mobile.data.dto.CreateMovementRequest
import com.osiris.mobile.data.dto.ImportOfxLineRequest
import com.osiris.mobile.data.dto.ImportOfxStatementRequest
import com.osiris.mobile.data.dto.MovementDto
import com.osiris.mobile.data.dto.OfxImportLineDto
import com.osiris.mobile.data.dto.OfxImportPreviewDto
import com.osiris.mobile.data.dto.OfxImportResultDto
import com.osiris.mobile.data.dto.StatementDto
import com.osiris.mobile.data.dto.UpdateAccountRequest
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.AccountEdit
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.domain.model.Movement
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.domain.model.OfxImportLine
import com.osiris.mobile.domain.model.OfxImportPreview
import com.osiris.mobile.domain.model.OfxImportResult
import com.osiris.mobile.domain.model.OfxImportSelection
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.AccountRepository

class AccountRepositoryImpl(
    private val api: AccountApi,
    private val bus: DataChangeBus,
) : AccountRepository {

    override suspend fun list(): OsirisResult<List<Account>> = osirisCatching {
        api.list().map { it.toDomain() }
    }

    override suspend fun get(id: String): OsirisResult<AccountEdit> = osirisCatching {
        api.get(id).toDomain()
    }

    override suspend fun create(name: String, type: AccountType, initialBalance: Double): OsirisResult<Unit> = osirisCatching {
        api.create(CreateAccountRequest(name, type.apiValue, initialBalance))
        bus.notify(DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
        Unit
    }

    override suspend fun update(id: String, name: String, type: AccountType): OsirisResult<Unit> = osirisCatching {
        api.update(id, UpdateAccountRequest(name, type.apiValue))
        bus.notify(DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun archive(id: String): OsirisResult<Unit> = osirisCatching {
        api.archive(id)
        bus.notify(DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun statement(id: String): OsirisResult<AccountStatement> = osirisCatching {
        api.statement(id).toDomain()
    }

    override suspend fun downloadStatementPdf(id: String): OsirisResult<StatementPdf> = osirisCatching {
        val response = api.downloadStatementPdf(id)
        StatementPdf(response.fileName, response.contentType, response.bytes)
    }

    override suspend fun createMovement(
        accountId: String,
        type: MovementType,
        amount: Double,
        occurredOn: String,
        description: String,
        categoryId: String?,
        notes: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.createMovement(
            accountId,
            CreateMovementRequest(type.apiValue, amount, occurredOn, description, categoryId, notes),
        )
        bus.notify(DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
        Unit
    }

    override suspend fun previewOfxImport(
        accountId: String,
        fileName: String,
        bytes: ByteArray,
    ): OsirisResult<OfxImportPreview> = osirisCatching {
        api.previewOfxImport(accountId, fileName, bytes).toDomain()
    }

    override suspend fun confirmOfxImport(
        accountId: String,
        selections: List<OfxImportSelection>,
    ): OsirisResult<OfxImportResult> = osirisCatching {
        val request = ImportOfxStatementRequest(selections.map { it.toRequest() })
        val result = api.confirmOfxImport(accountId, request).toDomain()
        bus.notify(DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
        result
    }
}

private fun AccountListItemDto.toDomain() =
    Account(id, name, AccountType.fromApi(type), currentBalance, isActive)

private fun AccountEditDto.toDomain() =
    AccountEdit(id, name, AccountType.fromApi(type), initialBalance)

private fun MovementDto.toDomain() =
    Movement(id, MovementType.fromApi(type), amount, isInflow, occurredOn, description, categoryId, notes)

private fun StatementDto.toDomain() =
    AccountStatement(id, name, AccountType.fromApi(type), initialBalance, currentBalance, isActive, movements.map { it.toDomain() })

private fun OfxImportLineDto.toDomain() =
    OfxImportLine(rowKey, externalId, occurredOn, amount, MovementType.fromApi(type), isInflow, description, isDuplicate)

private fun OfxImportPreviewDto.toDomain() =
    OfxImportPreview(accountId, accountName, periodStart, periodEnd, totalCount, newCount, duplicateCount, lines.map { it.toDomain() })

private fun OfxImportResultDto.toDomain() =
    OfxImportResult(imported, skippedDuplicates, total)

private fun OfxImportSelection.toRequest() =
    ImportOfxLineRequest(externalId, occurredOn, amount, type.apiValue, description, categoryId)
