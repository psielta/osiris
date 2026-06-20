package com.osiris.mobile.domain.model

data class OfxImportLine(
    val rowKey: String,
    val externalId: String,
    val occurredOn: String,
    val amount: Double,
    val type: MovementType,
    val isInflow: Boolean,
    val description: String,
    val isDuplicate: Boolean,
)

data class OfxImportPreview(
    val accountId: String,
    val accountName: String,
    val periodStart: String?,
    val periodEnd: String?,
    val totalCount: Int,
    val newCount: Int,
    val duplicateCount: Int,
    val lines: List<OfxImportLine>,
)

/** A line the user chose to import, carrying the (optional) category picked in the preview. */
data class OfxImportSelection(
    val externalId: String,
    val occurredOn: String,
    val amount: Double,
    val type: MovementType,
    val description: String,
    val categoryId: String?,
)

data class OfxImportResult(
    val imported: Int,
    val skippedDuplicates: Int,
    val total: Int,
)
