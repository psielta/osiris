package com.osiris.mobile.domain.model

enum class StatementStatus(val apiValue: Int) {
    Open(1),
    Closed(2),
    Paid(3),
    PartiallyPaid(4),
    Overdue(5);

    companion object {
        fun fromApi(value: Int): StatementStatus = entries.firstOrNull { it.apiValue == value } ?: Open
    }
}

data class CreditCard(
    val id: String,
    val name: String,
    val limit: Double,
    val closingDay: Int,
    val dueDay: Int,
    val isActive: Boolean,
)

data class CreditCardDetails(
    val id: String,
    val name: String,
    val limit: Double,
    val closingDay: Int,
    val dueDay: Int,
    val paymentAccountId: String?,
    val paymentAccountName: String?,
    val isActive: Boolean,
)

data class CreditCardOverview(
    val usedLimit: Double,
    val availableLimit: Double,
    val usagePercentage: Double,
    val futureInstallmentsTotal: Double,
    val nextStatement: CreditCardStatement?,
)

data class CreditCardPurchase(
    val id: String,
    val description: String,
    val categoryName: String?,
    val totalAmount: Double,
    val purchaseDate: String,
    val installments: Int,
)

data class CreditCardPurchaseDetails(
    val id: String,
    val creditCardId: String,
    val creditCardName: String,
    val categoryName: String?,
    val description: String,
    val totalAmount: Double,
    val purchaseDate: String,
    val installments: Int,
    val notes: String?,
    val installmentItems: List<CreditCardPurchaseInstallment>,
)

data class CreditCardPurchaseInstallment(
    val id: String,
    val installmentNumber: Int,
    val totalInstallments: Int,
    val amount: Double,
    val dueDate: String,
    val creditCardStatementId: String,
    val referenceMonth: Int,
    val referenceYear: Int,
)

data class CreditCardStatement(
    val id: String,
    val creditCardId: String,
    val referenceMonth: Int,
    val referenceYear: Int,
    val closingDate: String,
    val dueDate: String,
    val status: StatementStatus,
    val totalAmount: Double,
    val paidAmount: Double,
    val openBalance: Double,
)

data class CreditCardStatementDetails(
    val id: String,
    val creditCardId: String,
    val creditCardName: String,
    val referenceMonth: Int,
    val referenceYear: Int,
    val closingDate: String,
    val dueDate: String,
    val status: StatementStatus,
    val totalAmount: Double,
    val paidAmount: Double,
    val openBalance: Double,
    val installmentItems: List<CreditCardStatementInstallment>,
    val payments: List<CreditCardStatementPayment>,
)

data class CreditCardStatementInstallment(
    val id: String,
    val creditCardPurchaseId: String,
    val purchaseDescription: String,
    val installmentNumber: Int,
    val totalInstallments: Int,
    val amount: Double,
)

data class CreditCardStatementPayment(
    val id: String,
    val amount: Double,
    val paidAt: String,
    val financialAccountId: String?,
    val financialAccountName: String?,
    val notes: String?,
)

data class PurchasePreview(
    val installments: List<PurchasePreviewInstallment>,
    val totalAmount: Double,
    val limit: Double,
    val usedLimit: Double,
    val availableLimit: Double,
    val projectedUsedLimit: Double,
    val projectedUsagePercentage: Double,
    val highLimitUsage: Boolean,
    val projectedFutureInstallmentsTotal: Double,
)

data class PurchasePreviewInstallment(
    val number: Int,
    val amount: Double,
    val referenceMonth: Int,
    val referenceYear: Int,
    val dueDate: String,
)

data class StatementPdf(
    val fileName: String,
    val contentType: String,
    val bytes: ByteArray,
) {
    override fun equals(other: Any?): Boolean =
        other is StatementPdf &&
            fileName == other.fileName &&
            contentType == other.contentType &&
            bytes.contentEquals(other.bytes)

    override fun hashCode(): Int =
        31 * (31 * fileName.hashCode() + contentType.hashCode()) + bytes.contentHashCode()
}
