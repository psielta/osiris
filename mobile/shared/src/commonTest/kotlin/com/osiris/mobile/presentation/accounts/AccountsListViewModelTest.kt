package com.osiris.mobile.presentation.accounts

import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisError
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.DefaultDataChangeBus
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.AccountEdit
import com.osiris.mobile.domain.model.AccountStatement
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.domain.model.MovementType
import com.osiris.mobile.domain.model.OfxImportPreview
import com.osiris.mobile.domain.model.OfxImportResult
import com.osiris.mobile.domain.model.OfxImportSelection
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.AccountRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.cancel
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import kotlin.test.Test
import kotlin.test.assertEquals

@OptIn(ExperimentalCoroutinesApi::class)
class AccountsListViewModelTest {
    @Test
    fun reloads_when_accounts_scope_changes() = runTest {
        val dispatcher = StandardTestDispatcher(testScheduler)
        Dispatchers.setMain(dispatcher)
        val repository = FakeAccountRepository(
            responses = mutableListOf(
                listOf(Account("1", "Banco", AccountType.Checking, 100.0, isActive = true)),
                listOf(Account("2", "Caixa", AccountType.Cash, 200.0, isActive = true)),
            ),
        )
        val bus = DefaultDataChangeBus()
        val viewModel = AccountsListViewModel(repository, bus)

        try {
            advanceUntilIdle()

            assertEquals(1, repository.listCalls)
            assertEquals("Banco", viewModel.state.value.active.single().name)

            bus.notify(DataScope.Accounts)
            advanceUntilIdle()

            assertEquals(2, repository.listCalls)
            assertEquals("Caixa", viewModel.state.value.active.single().name)
        } finally {
            viewModel.viewModelScope.cancel()
            Dispatchers.resetMain()
        }
    }
}

private class FakeAccountRepository(
    private val responses: MutableList<List<Account>>,
) : AccountRepository {
    var listCalls = 0

    override suspend fun list(): OsirisResult<List<Account>> {
        listCalls += 1
        return OsirisResult.Success(responses.removeAt(0))
    }

    override suspend fun get(id: String): OsirisResult<AccountEdit> = unused()
    override suspend fun create(name: String, type: AccountType, initialBalance: Double): OsirisResult<Unit> = unused()
    override suspend fun update(id: String, name: String, type: AccountType): OsirisResult<Unit> = unused()
    override suspend fun archive(id: String): OsirisResult<Unit> = unused()
    override suspend fun statement(id: String): OsirisResult<AccountStatement> = unused()
    override suspend fun downloadStatementPdf(id: String): OsirisResult<StatementPdf> = unused()
    override suspend fun createMovement(
        accountId: String,
        type: MovementType,
        amount: Double,
        occurredOn: String,
        description: String,
        categoryId: String?,
        notes: String?,
    ): OsirisResult<Unit> = unused()

    override suspend fun previewOfxImport(
        accountId: String,
        fileName: String,
        bytes: ByteArray,
    ): OsirisResult<OfxImportPreview> = unused()

    override suspend fun confirmOfxImport(
        accountId: String,
        selections: List<OfxImportSelection>,
    ): OsirisResult<OfxImportResult> = unused()

    private fun <T> unused(): OsirisResult<T> =
        OsirisResult.Failure(OsirisError("Nao usado neste teste."))
}
