package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.CreditCard
import com.osiris.mobile.domain.model.CreditCardDetails
import com.osiris.mobile.domain.model.CreditCardOverview
import com.osiris.mobile.domain.model.CreditCardPurchase
import com.osiris.mobile.domain.model.CreditCardPurchaseDetails
import com.osiris.mobile.domain.model.CreditCardStatement
import com.osiris.mobile.domain.model.CreditCardStatementDetails
import com.osiris.mobile.domain.model.PurchasePreview
import com.osiris.mobile.domain.model.StatementPdf

interface CardRepository {
    suspend fun listCards(): OsirisResult<List<CreditCard>>
    suspend fun getCard(id: String): OsirisResult<CreditCardDetails>
    suspend fun createCard(
        name: String,
        limit: Double,
        closingDay: Int,
        dueDay: Int,
        paymentAccountId: String?,
    ): OsirisResult<Unit>
    suspend fun updateCard(
        id: String,
        name: String,
        limit: Double,
        closingDay: Int,
        dueDay: Int,
        paymentAccountId: String?,
    ): OsirisResult<Unit>
    suspend fun archiveCard(id: String): OsirisResult<Unit>
    suspend fun overview(cardId: String): OsirisResult<CreditCardOverview>
    suspend fun listPurchases(cardId: String): OsirisResult<List<CreditCardPurchase>>
    suspend fun getPurchase(cardId: String, purchaseId: String): OsirisResult<CreditCardPurchaseDetails>
    suspend fun previewPurchase(
        cardId: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
    ): OsirisResult<PurchasePreview?>
    suspend fun createPurchase(
        cardId: String,
        categoryId: String,
        description: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
        notes: String?,
    ): OsirisResult<Unit>
    suspend fun deletePurchase(cardId: String, purchaseId: String): OsirisResult<Unit>
    suspend fun listStatements(cardId: String): OsirisResult<List<CreditCardStatement>>
    suspend fun currentStatement(cardId: String): OsirisResult<CreditCardStatement?>
    suspend fun getStatement(cardId: String, statementId: String): OsirisResult<CreditCardStatementDetails>
    suspend fun payStatement(
        cardId: String,
        statementId: String,
        amount: Double,
        paidAt: String,
        financialAccountId: String?,
        notes: String?,
    ): OsirisResult<Unit>
    suspend fun downloadStatementPdf(cardId: String, statementId: String): OsirisResult<StatementPdf>
}
