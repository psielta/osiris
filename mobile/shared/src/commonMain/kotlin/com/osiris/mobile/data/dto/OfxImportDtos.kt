package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class OfxImportLineDto(
    val rowKey: String,
    val externalId: String,
    val occurredOn: String,
    val amount: Double,
    val type: Int,
    val isInflow: Boolean,
    val description: String,
    val isDuplicate: Boolean,
)

@Serializable
data class OfxImportPreviewDto(
    val accountId: String,
    val accountName: String,
    val bankId: String? = null,
    val accountNumber: String? = null,
    val currencyCode: String? = null,
    val periodStart: String? = null,
    val periodEnd: String? = null,
    val totalCount: Int,
    val newCount: Int,
    val duplicateCount: Int,
    val lines: List<OfxImportLineDto> = emptyList(),
)

@Serializable
data class ImportOfxLineRequest(
    val externalId: String,
    val occurredOn: String,
    val amount: Double,
    val type: Int,
    val description: String,
    val categoryId: String? = null,
)

@Serializable
data class ImportOfxStatementRequest(val lines: List<ImportOfxLineRequest>)

@Serializable
data class OfxImportResultDto(
    val imported: Int,
    val skippedDuplicates: Int,
    val total: Int,
)
