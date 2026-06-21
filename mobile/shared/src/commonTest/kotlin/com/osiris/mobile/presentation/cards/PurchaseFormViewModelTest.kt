package com.osiris.mobile.presentation.cards

import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisError
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.domain.model.CreditCard
import com.osiris.mobile.domain.model.CreditCardDetails
import com.osiris.mobile.domain.model.CreditCardOverview
import com.osiris.mobile.domain.model.CreditCardPurchase
import com.osiris.mobile.domain.model.CreditCardPurchaseDetails
import com.osiris.mobile.domain.model.CreditCardPurchaseOverview
import com.osiris.mobile.domain.model.CreditCardStatement
import com.osiris.mobile.domain.model.CreditCardStatementDetails
import com.osiris.mobile.domain.model.CreditCardStatementOverview
import com.osiris.mobile.domain.model.PurchasePreview
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.repository.CardRepository
import com.osiris.mobile.domain.repository.CategoryRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.cancel
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.TestCoroutineScheduler
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull

@OptIn(ExperimentalCoroutinesApi::class)
class PurchaseFormViewModelTest {

    @Test
    fun submit_perInstallment_multiplies_amount_by_installments() = runTest {
        val total = submitAndCaptureTotal(testScheduler, amount = "50", installments = "3", perInstallment = true)
        assertEquals(150.0, total, 0.0001)
    }

    @Test
    fun submit_perInstallment_rounds_to_two_decimals() = runTest {
        // 33.33 * 3 = 99.99000000000001 as a raw Double; must be rounded to 99.99 before sending.
        val total = submitAndCaptureTotal(testScheduler, amount = "33.33", installments = "3", perInstallment = true)
        assertEquals(99.99, total, 0.0001)
    }

    @Test
    fun submit_totalMode_sends_amount_unchanged() = runTest {
        val total = submitAndCaptureTotal(testScheduler, amount = "150", installments = "3", perInstallment = false)
        assertEquals(150.0, total, 0.0001)
    }

    private fun submitAndCaptureTotal(
        scheduler: TestCoroutineScheduler,
        amount: String,
        installments: String,
        perInstallment: Boolean,
    ): Double {
        val dispatcher = StandardTestDispatcher(scheduler)
        Dispatchers.setMain(dispatcher)
        val cards = FakeCardRepository()
        val viewModel = PurchaseFormViewModel(cards, FakeCategoryRepository(), cardId = "card-1")
        try {
            viewModel.onAmountModeChange(perInstallment)
            viewModel.onAmountChange(amount)
            viewModel.onInstallmentsChange(installments)
            viewModel.onDescriptionChange("Geladeira")
            viewModel.onCategoryChange("cat-1")
            viewModel.submit()
            scheduler.advanceUntilIdle()
            return assertNotNull(cards.createdTotalAmount, "createPurchase was not called")
        } finally {
            viewModel.viewModelScope.cancel()
            Dispatchers.resetMain()
        }
    }
}

private class FakeCardRepository : CardRepository {
    var createdTotalAmount: Double? = null

    override suspend fun createPurchase(
        cardId: String,
        categoryId: String,
        description: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
        notes: String?,
    ): OsirisResult<Unit> {
        createdTotalAmount = totalAmount
        return OsirisResult.Success(Unit)
    }

    override suspend fun previewPurchase(
        cardId: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
    ): OsirisResult<PurchasePreview?> = OsirisResult.Success(null)

    override suspend fun listCards(): OsirisResult<List<CreditCard>> = unused()
    override suspend fun getCard(id: String): OsirisResult<CreditCardDetails> = unused()
    override suspend fun createCard(name: String, limit: Double, closingDay: Int, dueDay: Int, paymentAccountId: String?): OsirisResult<Unit> = unused()
    override suspend fun updateCard(id: String, name: String, limit: Double, closingDay: Int, dueDay: Int, paymentAccountId: String?): OsirisResult<Unit> = unused()
    override suspend fun archiveCard(id: String): OsirisResult<Unit> = unused()
    override suspend fun overview(cardId: String): OsirisResult<CreditCardOverview> = unused()
    override suspend fun listPurchases(cardId: String): OsirisResult<List<CreditCardPurchase>> = unused()
    override suspend fun listAllPurchases(from: String, to: String): OsirisResult<List<CreditCardPurchaseOverview>> = unused()
    override suspend fun getPurchase(cardId: String, purchaseId: String): OsirisResult<CreditCardPurchaseDetails> = unused()
    override suspend fun deletePurchase(cardId: String, purchaseId: String): OsirisResult<Unit> = unused()
    override suspend fun listStatements(cardId: String): OsirisResult<List<CreditCardStatement>> = unused()
    override suspend fun listAllStatements(from: String, to: String): OsirisResult<List<CreditCardStatementOverview>> = unused()
    override suspend fun currentStatement(cardId: String): OsirisResult<CreditCardStatement?> = unused()
    override suspend fun getStatement(cardId: String, statementId: String): OsirisResult<CreditCardStatementDetails> = unused()
    override suspend fun payStatement(cardId: String, statementId: String, amount: Double, paidAt: String, financialAccountId: String?, notes: String?): OsirisResult<Unit> = unused()
    override suspend fun downloadStatementPdf(cardId: String, statementId: String): OsirisResult<StatementPdf> = unused()

    private fun <T> unused(): OsirisResult<T> = OsirisResult.Failure(OsirisError("Nao usado neste teste."))
}

private class FakeCategoryRepository : CategoryRepository {
    override suspend fun list(): OsirisResult<List<Category>> = OsirisResult.Success(emptyList())
    override suspend fun get(id: String): OsirisResult<Category> = unused()
    override suspend fun create(name: String, type: CategoryType, color: String?): OsirisResult<Unit> = unused()
    override suspend fun update(id: String, name: String, type: CategoryType, color: String?): OsirisResult<Unit> = unused()
    override suspend fun archive(id: String): OsirisResult<Unit> = unused()
    override suspend fun delete(id: String): OsirisResult<Unit> = unused()

    private fun <T> unused(): OsirisResult<T> = OsirisResult.Failure(OsirisError("Nao usado neste teste."))
}
