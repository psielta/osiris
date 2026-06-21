package com.osiris.mobile.domain.model

/** An existing movement offered as a reconciliation match for an imported line, best-first. */
data class ReconciliationCandidate(
    val movementId: String,
    val occurredOn: String,
    val amount: Double,
    val isInflow: Boolean,
    val description: String,
)

data class OfxImportLine(
    val rowKey: String,
    val externalId: String,
    val occurredOn: String,
    val amount: Double,
    val type: MovementType,
    val isInflow: Boolean,
    val description: String,
    val isDuplicate: Boolean,
    val suggestedMovementId: String? = null,
    val candidates: List<ReconciliationCandidate> = emptyList(),
)

data class OfxImportPreview(
    val accountId: String,
    val accountName: String,
    val periodStart: String?,
    val periodEnd: String?,
    val totalCount: Int,
    val newCount: Int,
    val duplicateCount: Int,
    val suggestedReconciliationCount: Int,
    val lines: List<OfxImportLine>,
)

/**
 * A line the user chose to import, carrying the (optional) category picked in the preview. When
 * [reconcileWithMovementId] is set, the line is linked to that existing movement instead of creating a new one.
 */
data class OfxImportSelection(
    val externalId: String,
    val occurredOn: String,
    val amount: Double,
    val type: MovementType,
    val description: String,
    val categoryId: String?,
    val reconcileWithMovementId: String? = null,
)

data class OfxImportResult(
    val imported: Int,
    val reconciled: Int,
    val skippedDuplicates: Int,
    val total: Int,
)
