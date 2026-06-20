package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.CardDetailsDto
import com.osiris.mobile.data.dto.CardListItemDto
import com.osiris.mobile.data.dto.CardOverviewDto
import com.osiris.mobile.data.dto.CreateCardRequest
import com.osiris.mobile.data.dto.CreatePurchaseRequest
import com.osiris.mobile.data.dto.PurchaseDetailsDto
import com.osiris.mobile.data.dto.PurchaseInstallmentDto
import com.osiris.mobile.data.dto.PurchaseListItemDto
import com.osiris.mobile.data.dto.PurchaseOverviewDto
import com.osiris.mobile.data.dto.PurchasePreviewDto
import com.osiris.mobile.data.dto.PurchasePreviewInstallmentDto
import com.osiris.mobile.data.dto.RegisterStatementPaymentRequest
import com.osiris.mobile.data.dto.StatementDetailsDto
import com.osiris.mobile.data.dto.StatementInstallmentDto
import com.osiris.mobile.data.dto.StatementListItemDto
import com.osiris.mobile.data.dto.StatementOverviewDto
import com.osiris.mobile.data.dto.StatementPaymentDto
import com.osiris.mobile.data.dto.UpdateCardRequest
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.CardApi
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.domain.model.CreditCard
import com.osiris.mobile.domain.model.CreditCardDetails
import com.osiris.mobile.domain.model.CreditCardOverview
import com.osiris.mobile.domain.model.CreditCardPurchase
import com.osiris.mobile.domain.model.CreditCardPurchaseDetails
import com.osiris.mobile.domain.model.CreditCardPurchaseInstallment
import com.osiris.mobile.domain.model.CreditCardPurchaseOverview
import com.osiris.mobile.domain.model.CreditCardStatement
import com.osiris.mobile.domain.model.CreditCardStatementDetails
import com.osiris.mobile.domain.model.CreditCardStatementInstallment
import com.osiris.mobile.domain.model.CreditCardStatementOverview
import com.osiris.mobile.domain.model.CreditCardStatementPayment
import com.osiris.mobile.domain.model.PurchasePreview
import com.osiris.mobile.domain.model.PurchasePreviewInstallment
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.model.StatementStatus
import com.osiris.mobile.domain.repository.CardRepository

class CardRepositoryImpl(
    private val api: CardApi,
    private val bus: DataChangeBus,
) : CardRepository {

    override suspend fun listCards(): OsirisResult<List<CreditCard>> = osirisCatching {
        api.listCards().map { it.toDomain() }
    }

    override suspend fun getCard(id: String): OsirisResult<CreditCardDetails> = osirisCatching {
        api.getCard(id).toDomain()
    }

    override suspend fun createCard(
        name: String,
        limit: Double,
        closingDay: Int,
        dueDay: Int,
        paymentAccountId: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.createCard(CreateCardRequest(name, limit, closingDay, dueDay, paymentAccountId))
        bus.notify(DataScope.Cards, DataScope.Dashboard, DataScope.Reports)
        Unit
    }

    override suspend fun updateCard(
        id: String,
        name: String,
        limit: Double,
        closingDay: Int,
        dueDay: Int,
        paymentAccountId: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.updateCard(id, UpdateCardRequest(name, limit, closingDay, dueDay, paymentAccountId))
        bus.notify(DataScope.Cards, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun archiveCard(id: String): OsirisResult<Unit> = osirisCatching {
        api.archiveCard(id)
        bus.notify(DataScope.Cards, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun overview(cardId: String): OsirisResult<CreditCardOverview> = osirisCatching {
        api.overview(cardId).toDomain()
    }

    override suspend fun listPurchases(cardId: String): OsirisResult<List<CreditCardPurchase>> = osirisCatching {
        api.listPurchases(cardId).map { it.toDomain() }
    }

    override suspend fun listAllPurchases(
        from: String,
        to: String,
    ): OsirisResult<List<CreditCardPurchaseOverview>> = osirisCatching {
        api.listAllPurchases(from, to).map { it.toDomain() }
    }

    override suspend fun getPurchase(cardId: String, purchaseId: String): OsirisResult<CreditCardPurchaseDetails> =
        osirisCatching {
            api.getPurchase(cardId, purchaseId).toDomain()
        }

    override suspend fun previewPurchase(
        cardId: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
    ): OsirisResult<PurchasePreview?> = osirisCatching {
        api.previewPurchase(cardId, totalAmount, purchaseDate, installments)?.toDomain()
    }

    override suspend fun createPurchase(
        cardId: String,
        categoryId: String,
        description: String,
        totalAmount: Double,
        purchaseDate: String,
        installments: Int,
        notes: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.createPurchase(
            cardId,
            CreatePurchaseRequest(categoryId, description, totalAmount, purchaseDate, installments, notes),
        )
        bus.notify(DataScope.Cards, DataScope.Dashboard, DataScope.Reports)
        Unit
    }

    override suspend fun deletePurchase(cardId: String, purchaseId: String): OsirisResult<Unit> = osirisCatching {
        api.deletePurchase(cardId, purchaseId)
        bus.notify(DataScope.Cards, DataScope.Dashboard, DataScope.Reports)
    }

    override suspend fun listStatements(cardId: String): OsirisResult<List<CreditCardStatement>> = osirisCatching {
        api.listStatements(cardId).map { it.toDomain() }
    }

    override suspend fun listAllStatements(
        from: String,
        to: String,
    ): OsirisResult<List<CreditCardStatementOverview>> = osirisCatching {
        api.listAllStatements(from, to).map { it.toDomain() }
    }

    override suspend fun currentStatement(cardId: String): OsirisResult<CreditCardStatement?> = osirisCatching {
        api.currentStatement(cardId)?.toDomain()
    }

    override suspend fun getStatement(cardId: String, statementId: String): OsirisResult<CreditCardStatementDetails> =
        osirisCatching {
            api.getStatement(cardId, statementId).toDomain()
        }

    override suspend fun payStatement(
        cardId: String,
        statementId: String,
        amount: Double,
        paidAt: String,
        financialAccountId: String?,
        notes: String?,
    ): OsirisResult<Unit> = osirisCatching {
        api.payStatement(cardId, statementId, RegisterStatementPaymentRequest(amount, paidAt, financialAccountId, notes))
        bus.notify(DataScope.Cards, DataScope.Accounts, DataScope.Dashboard, DataScope.Reports)
        Unit
    }

    override suspend fun downloadStatementPdf(cardId: String, statementId: String): OsirisResult<StatementPdf> =
        osirisCatching {
            val response = api.downloadStatementPdf(cardId, statementId)
            StatementPdf(response.fileName, response.contentType, response.bytes)
        }
}

private fun CardListItemDto.toDomain() =
    CreditCard(id, name, limit, closingDay, dueDay, isActive)

private fun CardDetailsDto.toDomain() =
    CreditCardDetails(id, name, limit, closingDay, dueDay, paymentAccountId, paymentAccountName, isActive)

private fun CardOverviewDto.toDomain() =
    CreditCardOverview(usedLimit, availableLimit, usagePercentage, futureInstallmentsTotal, nextStatement?.toDomain())

private fun PurchaseListItemDto.toDomain() =
    CreditCardPurchase(id, description, categoryName, totalAmount, purchaseDate, installments)

private fun PurchaseOverviewDto.toDomain() =
    CreditCardPurchaseOverview(
        id,
        creditCardId,
        creditCardName,
        description,
        categoryName,
        totalAmount,
        purchaseDate,
        installments,
    )

private fun PurchaseInstallmentDto.toDomain() =
    CreditCardPurchaseInstallment(
        id,
        installmentNumber,
        totalInstallments,
        amount,
        dueDate,
        creditCardStatementId,
        referenceMonth,
        referenceYear,
    )

private fun PurchaseDetailsDto.toDomain() =
    CreditCardPurchaseDetails(
        id,
        creditCardId,
        creditCardName,
        categoryName,
        description,
        totalAmount,
        purchaseDate,
        installments,
        notes,
        installmentItems.map { it.toDomain() },
    )

private fun StatementListItemDto.toDomain() =
    CreditCardStatement(
        id,
        creditCardId,
        referenceMonth,
        referenceYear,
        closingDate,
        dueDate,
        StatementStatus.fromApi(status),
        totalAmount,
        paidAmount,
        openBalance,
    )

private fun StatementOverviewDto.toDomain() =
    CreditCardStatementOverview(
        id,
        creditCardId,
        creditCardName,
        referenceMonth,
        referenceYear,
        closingDate,
        dueDate,
        StatementStatus.fromApi(status),
        totalAmount,
        paidAmount,
        openBalance,
    )

private fun StatementInstallmentDto.toDomain() =
    CreditCardStatementInstallment(
        id,
        creditCardPurchaseId,
        purchaseDescription,
        installmentNumber,
        totalInstallments,
        amount,
    )

private fun StatementPaymentDto.toDomain() =
    CreditCardStatementPayment(id, amount, paidAt, financialAccountId, financialAccountName, notes)

private fun StatementDetailsDto.toDomain() =
    CreditCardStatementDetails(
        id,
        creditCardId,
        creditCardName,
        referenceMonth,
        referenceYear,
        closingDate,
        dueDate,
        StatementStatus.fromApi(status),
        totalAmount,
        paidAmount,
        openBalance,
        installmentItems.map { it.toDomain() },
        payments.map { it.toDomain() },
    )

private fun PurchasePreviewInstallmentDto.toDomain() =
    PurchasePreviewInstallment(number, amount, referenceMonth, referenceYear, dueDate)

private fun PurchasePreviewDto.toDomain() =
    PurchasePreview(
        installments.map { it.toDomain() },
        totalAmount,
        limit,
        usedLimit,
        availableLimit,
        projectedUsedLimit,
        projectedUsagePercentage,
        highLimitUsage,
        projectedFutureInstallmentsTotal,
    )
