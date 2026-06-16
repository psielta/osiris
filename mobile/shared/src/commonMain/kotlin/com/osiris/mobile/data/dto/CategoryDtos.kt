package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class CategoryListItemDto(
    val id: String,
    val name: String,
    val type: Int,
    val color: String? = null,
    val isActive: Boolean,
)

@Serializable
data class CategoryEditDto(
    val id: String,
    val name: String,
    val type: Int,
    val color: String? = null,
)

@Serializable
data class CreateCategoryRequest(
    val name: String,
    val type: Int,
    val color: String? = null,
)

@Serializable
data class UpdateCategoryRequest(
    val name: String,
    val type: Int,
    val color: String? = null,
)

@Serializable
data class CreatedCategoryResponse(val id: String)
