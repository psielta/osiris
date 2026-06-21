package com.osiris.mobile.presentation.accounts

import com.osiris.mobile.domain.model.OfxImportLine

/** What to do with an imported statement line on confirm. Shared by the OFX/CSV/PDF import screens. */
enum class ImportLineAction { New, Reconcile, Ignore }

/** Default action for a freshly previewed line: ignore duplicates, reconcile when suggested, else import. */
fun initialImportAction(line: OfxImportLine): ImportLineAction = when {
    line.isDuplicate -> ImportLineAction.Ignore
    line.suggestedMovementId != null -> ImportLineAction.Reconcile
    else -> ImportLineAction.New
}
