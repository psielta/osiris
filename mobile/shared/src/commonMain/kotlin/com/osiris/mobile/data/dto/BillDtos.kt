package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class BillListItemDto(
    val id: String,
    val description: String,
    val amount: Double,
    val dueDate: String,
    val paidAt: String? = null,
    val status: Int,
    val categoryId: String,
    val categoryName: String? = null,
    val categoryColor: String? = null,
    val paymentAccountId: String? = null,
    val paymentAccountName: String? = null,
)

@Serializable
data class BillDetailsDto(
    val id: String,
    val description: String,
    val amount: Double,
    val dueDate: String,
    val paidAt: String? = null,
    val status: Int,
    val categoryId: String,
    val categoryName: String? = null,
    val categoryColor: String? = null,
    val paymentAccountId: String? = null,
    val paymentAccountName: String? = null,
    val notes: String? = null,
)

@Serializable
data class CreateBillRequest(
    val description: String,
    val amount: Double,
    val dueDate: String,
    val categoryId: String,
    val paymentAccountId: String? = null,
    val notes: String? = null,
)

@Serializable
data class UpdateBillRequest(
    val description: String,
    val amount: Double,
    val dueDate: String,
    val categoryId: String,
    val paymentAccountId: String? = null,
    val notes: String? = null,
)

@Serializable
data class PayBillRequest(
    val paidAt: String,
    val paymentAccountId: String? = null,
)
