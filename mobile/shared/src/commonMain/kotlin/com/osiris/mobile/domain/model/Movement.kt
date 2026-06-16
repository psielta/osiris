package com.osiris.mobile.domain.model

enum class MovementType(val apiValue: Int) {
    Income(1),
    Expense(2),
    BillPayment(3),
    CreditCardStatementPayment(4),
    TransferIn(5),
    TransferOut(6),
    Adjustment(7);

    companion object {
        fun fromApi(value: Int): MovementType = entries.firstOrNull { it.apiValue == value } ?: Adjustment
    }
}

data class Movement(
    val id: String,
    val type: MovementType,
    val amount: Double,
    val isInflow: Boolean,
    val occurredOn: String,
    val description: String,
    val categoryId: String?,
    val notes: String?,
)

data class AccountStatement(
    val id: String,
    val name: String,
    val type: AccountType,
    val initialBalance: Double,
    val currentBalance: Double,
    val isActive: Boolean,
    val movements: List<Movement>,
)
