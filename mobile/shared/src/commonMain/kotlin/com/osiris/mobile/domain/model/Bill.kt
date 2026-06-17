package com.osiris.mobile.domain.model

enum class BillStatus(val apiValue: Int) {
    Pending(1),
    Paid(2),
    Overdue(3);

    companion object {
        fun fromApi(value: Int): BillStatus = entries.firstOrNull { it.apiValue == value } ?: Pending
    }
}

data class Bill(
    val id: String,
    val description: String,
    val amount: Double,
    val dueDate: String,
    val paidAt: String?,
    val status: BillStatus,
    val categoryId: String,
    val categoryName: String?,
    val categoryColor: String?,
    val paymentAccountId: String?,
    val paymentAccountName: String?,
)

data class BillDetails(
    val id: String,
    val description: String,
    val amount: Double,
    val dueDate: String,
    val paidAt: String?,
    val status: BillStatus,
    val categoryId: String,
    val categoryName: String?,
    val categoryColor: String?,
    val paymentAccountId: String?,
    val paymentAccountName: String?,
    val notes: String?,
)
