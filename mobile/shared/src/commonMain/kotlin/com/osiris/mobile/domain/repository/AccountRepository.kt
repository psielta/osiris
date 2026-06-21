package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.AccountEdit
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.domain.model.CsvAnalysis
import com.osiris.mobile.domain.model.CsvImportMapping
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.domain.model.OfxImportPreview
import com.osiris.mobile.domain.model.OfxImportResult
import com.osiris.mobile.domain.model.OfxImportSelection
import com.osiris.mobile.domain.model.StatementPdf

interface AccountRepository {
    suspend fun list(): OsirisResult<List<Account>>
    suspend fun get(id: String): OsirisResult<AccountEdit>
    suspend fun create(name: String, type: AccountType, initialBalance: Double): OsirisResult<Unit>
    suspend fun update(id: String, name: String, type: AccountType): OsirisResult<Unit>
    suspend fun archive(id: String): OsirisResult<Unit>
    suspend fun statement(id: String): OsirisResult<AccountStatement>
    suspend fun downloadStatementPdf(id: String): OsirisResult<StatementPdf>
    suspend fun createMovement(
        accountId: String,
        type: MovementType,
        amount: Double,
        occurredOn: String,
        description: String,
        categoryId: String?,
        notes: String?,
    ): OsirisResult<Unit>

    suspend fun previewOfxImport(accountId: String, fileName: String, bytes: ByteArray): OsirisResult<OfxImportPreview>

    suspend fun previewPdfImport(accountId: String, fileName: String, bytes: ByteArray): OsirisResult<OfxImportPreview>

    suspend fun confirmOfxImport(accountId: String, selections: List<OfxImportSelection>): OsirisResult<OfxImportResult>

    suspend fun analyzeCsvImport(
        accountId: String,
        fileName: String,
        bytes: ByteArray,
        delimiter: String? = null,
        encoding: String? = null,
    ): OsirisResult<CsvAnalysis>

    suspend fun previewCsvImport(
        accountId: String,
        fileName: String,
        bytes: ByteArray,
        mapping: CsvImportMapping,
    ): OsirisResult<OfxImportPreview>
}
