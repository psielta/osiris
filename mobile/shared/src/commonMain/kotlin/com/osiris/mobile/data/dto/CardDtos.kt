package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class CardListItemDto(
    val id: String,
    val name: String,
    val limit: Double,
    val closingDay: Int,
    val dueDay: Int,
    val isActive: Boolean,
)

@Serializable
data class CardDetailsDto(
    val id: String,
    val name: String,
    val limit: Double,
    val closingDay: Int,
    val dueDay: Int,
    val paymentAccountId: String? = null,
    val paymentAccountName: String? = null,
    val isActive: Boolean,
)

@Serializable
data class CardOverviewDto(
    val usedLimit: Double,
    val availableLimit: Double,
    val usagePercentage: Double,
    val futureInstallmentsTotal: Double,
    val nextStatement: StatementListItemDto? = null,
)

@Serializable
data class PurchaseListItemDto(
    val id: String,
    val description: String,
    val categoryName: String? = null,
    val totalAmount: Double,
    val purchaseDate: String,
    val installments: Int,
)

@Serializable
data class PurchaseOverviewDto(
    val id: String,
    val creditCardId: String,
    val creditCardName: String,
    val description: String,
    val categoryName: String? = null,
    val totalAmount: Double,
    val purchaseDate: String,
    val installments: Int,
)

@Serializable
data class PurchaseInstallmentDto(
    val id: String,
    val installmentNumber: Int,
    val totalInstallments: Int,
    val amount: Double,
    val dueDate: String,
    val creditCardStatementId: String,
    val referenceMonth: Int,
    val referenceYear: Int,
)

@Serializable
data class PurchaseDetailsDto(
    val id: String,
    val creditCardId: String,
    val creditCardName: String,
    val categoryName: String? = null,
    val description: String,
    val totalAmount: Double,
    val purchaseDate: String,
    val installments: Int,
    val notes: String? = null,
    val installmentItems: List<PurchaseInstallmentDto> = emptyList(),
)

@Serializable
data class StatementListItemDto(
    val id: String,
    val creditCardId: String,
    val referenceMonth: Int,
    val referenceYear: Int,
    val closingDate: String,
    val dueDate: String,
    val status: Int,
    val totalAmount: Double,
    val paidAmount: Double,
    val openBalance: Double,
)

@Serializable
data class StatementOverviewDto(
    val id: String,
    val creditCardId: String,
    val creditCardName: String,
    val referenceMonth: Int,
    val referenceYear: Int,
    val closingDate: String,
    val dueDate: String,
    val status: Int,
    val totalAmount: Double,
    val paidAmount: Double,
    val openBalance: Double,
)

@Serializable
data class StatementInstallmentDto(
    val id: String,
    val creditCardPurchaseId: String,
    val purchaseDescription: String,
    val installmentNumber: Int,
    val totalInstallments: Int,
    val amount: Double,
)

@Serializable
data class StatementPaymentDto(
    val id: String,
    val amount: Double,
    val paidAt: String,
    val financialAccountId: String? = null,
    val financialAccountName: String? = null,
    val notes: String? = null,
)

@Serializable
data class StatementDetailsDto(
    val id: String,
    val creditCardId: String,
    val creditCardName: String,
    val referenceMonth: Int,
    val referenceYear: Int,
    val closingDate: String,
    val dueDate: String,
    val status: Int,
    val totalAmount: Double,
    val paidAmount: Double,
    val openBalance: Double,
    val installmentItems: List<StatementInstallmentDto> = emptyList(),
    val payments: List<StatementPaymentDto> = emptyList(),
)

@Serializable
data class PurchasePreviewInstallmentDto(
    val number: Int,
    val amount: Double,
    val referenceMonth: Int,
    val referenceYear: Int,
    val dueDate: String,
)

@Serializable
data class PurchasePreviewDto(
    val installments: List<PurchasePreviewInstallmentDto> = emptyList(),
    val totalAmount: Double,
    val limit: Double,
    val usedLimit: Double,
    val availableLimit: Double,
    val projectedUsedLimit: Double,
    val projectedUsagePercentage: Double,
    val highLimitUsage: Boolean,
    val projectedFutureInstallmentsTotal: Double,
)

@Serializable
data class CreateCardRequest(
    val name: String,
    val limit: Double,
    val closingDay: Int,
    val dueDay: Int,
    val paymentAccountId: String? = null,
)

@Serializable
data class UpdateCardRequest(
    val name: String,
    val limit: Double,
    val closingDay: Int,
    val dueDay: Int,
    val paymentAccountId: String? = null,
)

@Serializable
data class CreatePurchaseRequest(
    val categoryId: String,
    val description: String,
    val totalAmount: Double,
    val purchaseDate: String,
    val installments: Int,
    val notes: String? = null,
)

@Serializable
data class RegisterStatementPaymentRequest(
    val amount: Double,
    val paidAt: String,
    val financialAccountId: String? = null,
    val notes: String? = null,
)
