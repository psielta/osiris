package com.osiris.mobile.android.feature.accounts

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisDropdownField
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.OfxImportLine
import com.osiris.mobile.domain.model.ReconciliationCandidate
import com.osiris.mobile.presentation.accounts.ImportLineAction
import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val importInflowColor = Color(0xFF16A34A)
private val importRowDateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy")

/**
 * A single statement line in the import preview, shared by the OFX/CSV/PDF screens. Shows the line and,
 * for non-duplicates, an action selector (import / reconcile / ignore) plus the matching follow-up control
 * (candidate picker when reconciling, category picker when importing as new). Duplicates render read-only.
 */
@Composable
fun ImportPreviewLineRow(
    line: OfxImportLine,
    action: ImportLineAction,
    reconcileWithMovementId: String?,
    categoryId: String?,
    categories: List<Category>,
    noCategoryLabel: String,
    onAction: (ImportLineAction) -> Unit,
    onReconcileTarget: (String?) -> Unit,
    onCategory: (String?) -> Unit,
) {
    val sign = if (line.isInflow) "+" else "−"
    val dateLabel = if (line.isDuplicate) {
        formatImportDate(line.occurredOn) + " · " + stringResource(R.string.import_ofx_already_imported)
    } else {
        formatImportDate(line.occurredOn)
    }

    Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {
                Text(line.description, style = MaterialTheme.typography.bodyLarge)
                Text(
                    text = dateLabel,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                if (line.suggestedMovementId != null && action == ImportLineAction.Reconcile) {
                    Text(
                        text = stringResource(R.string.import_reconcile_suggested),
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.primary,
                    )
                }
            }
            Text(
                text = "$sign${Money.brl(line.amount)}",
                style = MaterialTheme.typography.bodyLarge,
                color = if (line.isInflow) importInflowColor else MaterialTheme.colorScheme.error,
            )
        }

        if (!line.isDuplicate) {
            Spacer(Modifier.height(8.dp))
            val newLabel = stringResource(R.string.import_action_new)
            val reconcileLabel = stringResource(R.string.import_action_reconcile)
            val ignoreLabel = stringResource(R.string.import_action_ignore)
            val actionOptions = if (line.candidates.isEmpty()) {
                listOf(ImportLineAction.New, ImportLineAction.Ignore)
            } else {
                listOf(ImportLineAction.New, ImportLineAction.Reconcile, ImportLineAction.Ignore)
            }
            OsirisDropdownField(
                label = stringResource(R.string.import_action_label),
                selected = action,
                options = actionOptions,
                optionLabel = {
                    when (it) {
                        ImportLineAction.New -> newLabel
                        ImportLineAction.Reconcile -> reconcileLabel
                        ImportLineAction.Ignore -> ignoreLabel
                    }
                },
                onSelect = onAction,
            )

            when (action) {
                ImportLineAction.Reconcile -> if (line.candidates.isNotEmpty()) {
                    Spacer(Modifier.height(8.dp))
                    val selectedCandidate = line.candidates.find { it.movementId == reconcileWithMovementId }
                        ?: line.candidates.first()
                    OsirisDropdownField(
                        label = stringResource(R.string.import_reconcile_with_label),
                        selected = selectedCandidate,
                        options = line.candidates,
                        optionLabel = { candidateLabel(it) },
                        onSelect = { onReconcileTarget(it.movementId) },
                    )
                }

                ImportLineAction.New -> {
                    Spacer(Modifier.height(8.dp))
                    val categoryOptions = listOf<Category?>(null) + categories
                    val selectedCategory = categories.find { it.id == categoryId }
                    OsirisDropdownField(
                        label = stringResource(R.string.movement_category_label),
                        selected = selectedCategory,
                        options = categoryOptions,
                        optionLabel = { it?.name ?: noCategoryLabel },
                        onSelect = { onCategory(it?.id) },
                    )
                }

                ImportLineAction.Ignore -> Unit
            }
        }
    }
}

private fun candidateLabel(candidate: ReconciliationCandidate): String {
    val sign = if (candidate.isInflow) "+" else "−"
    return "${formatImportDate(candidate.occurredOn)} · ${candidate.description} · $sign${Money.brl(candidate.amount)}"
}

private fun formatImportDate(iso: String): String =
    runCatching { LocalDate.parse(iso).format(importRowDateFormat) }.getOrDefault(iso)
