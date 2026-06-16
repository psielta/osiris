package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class AccountListItemDto(
    val id: String,
    val name: String,
    val type: Int,
    val currentBalance: Double,
    val isActive: Boolean,
)

@Serializable
data class AccountEditDto(
    val id: String,
    val name: String,
    val type: Int,
    val initialBalance: Double,
)

@Serializable
data class MovementDto(
    val id: String,
    val type: Int,
    val amount: Double,
    val isInflow: Boolean,
    val occurredOn: String,
    val description: String,
    val categoryId: String? = null,
    val notes: String? = null,
)

@Serializable
data class StatementDto(
    val id: String,
    val name: String,
    val type: Int,
    val initialBalance: Double,
    val currentBalance: Double,
    val isActive: Boolean,
    val movements: List<MovementDto> = emptyList(),
)

@Serializable
data class CreateAccountRequest(val name: String, val type: Int, val initialBalance: Double)

@Serializable
data class UpdateAccountRequest(val name: String, val type: Int)

@Serializable
data class CreateMovementRequest(
    val type: Int,
    val amount: Double,
    val occurredOn: String,
    val description: String,
    val categoryId: String? = null,
    val notes: String? = null,
)

@Serializable
data class CreatedIdResponse(val id: String)
