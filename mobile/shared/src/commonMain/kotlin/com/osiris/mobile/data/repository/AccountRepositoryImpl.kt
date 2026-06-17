package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.AccountEditDto
import com.osiris.mobile.data.dto.AccountListItemDto
import com.osiris.mobile.data.dto.CreateAccountRequest
import com.osiris.mobile.data.dto.CreateMovementRequest
import com.osiris.mobile.data.dto.MovementDto
import com.osiris.mobile.data.dto.StatementDto
import com.osiris.mobile.data.dto.UpdateAccountRequest
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.AccountEdit
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.domain.model.Movement
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.AccountRepository

class AccountRepositoryImpl(private val api: AccountApi) : AccountRepository {

    override suspend fun list(): OsirisResult<List<Account>> = osirisCatching {
        api.list().map { it.toDomain() }
    }

    override suspend fun get(id: String): OsirisResult<AccountEdit> = osirisCatching {
        api.get(id).toDomain()
    }

    override suspend fun create(name: String, type: AccountType, initialBalance: Double): OsirisResult<Unit> = osirisCatching {
        api.create(CreateAccountRequest(name, type.apiValue, initialBalance))
        Unit
    }

    override suspend fun update(id: String, name: String, type: AccountType): OsirisResult<Unit> = osirisCatching {
        api.update(id, UpdateAccountRequest(name, type.apiValue))
    }

    override suspend fun archive(id: String): OsirisResult<Unit> = osirisCatching {
        api.archive(id)
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
        Unit
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
