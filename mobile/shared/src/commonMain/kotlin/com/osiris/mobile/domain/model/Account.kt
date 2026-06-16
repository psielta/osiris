package com.osiris.mobile.domain.model

enum class AccountType(val apiValue: Int) {
    Checking(1),
    Savings(2),
    Cash(3),
    Other(4);

    companion object {
        fun fromApi(value: Int): AccountType = entries.firstOrNull { it.apiValue == value } ?: Other
    }
}

data class Account(
    val id: String,
    val name: String,
    val type: AccountType,
    val currentBalance: Double,
    val isActive: Boolean,
)

data class AccountEdit(
    val id: String,
    val name: String,
    val type: AccountType,
    val initialBalance: Double,
)
