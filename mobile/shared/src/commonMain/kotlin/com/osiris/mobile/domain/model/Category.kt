package com.osiris.mobile.domain.model

enum class CategoryType(val apiValue: Int) {
    Income(1),
    Expense(2);

    companion object {
        fun fromApi(value: Int): CategoryType = entries.firstOrNull { it.apiValue == value } ?: Expense
    }
}

data class Category(
    val id: String,
    val name: String,
    val type: CategoryType,
    val color: String?,
    val isActive: Boolean,
)
