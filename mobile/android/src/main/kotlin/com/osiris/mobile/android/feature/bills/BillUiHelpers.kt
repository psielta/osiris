package com.osiris.mobile.android.feature.bills

import androidx.compose.runtime.Composable
import androidx.compose.ui.res.stringResource
import com.osiris.mobile.android.R
import com.osiris.mobile.domain.model.BillStatus
import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val dateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy")

internal fun formatDate(iso: String): String =
    runCatching { LocalDate.parse(iso).format(dateFormat) }.getOrDefault(iso)

@Composable
internal fun billStatusLabel(status: BillStatus): String = stringResource(
    when (status) {
        BillStatus.Pending -> R.string.bill_status_pending
        BillStatus.Paid -> R.string.bill_status_paid
        BillStatus.Overdue -> R.string.bill_status_overdue
    },
)
