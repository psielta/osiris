package com.osiris.mobile.android.feature.cards

import android.content.Context
import android.content.Intent
import android.content.ActivityNotFoundException
import androidx.compose.runtime.Composable
import androidx.compose.ui.res.stringResource
import androidx.core.content.FileProvider
import com.osiris.mobile.android.R
import com.osiris.mobile.domain.model.StatementPdf
import com.osiris.mobile.domain.model.StatementStatus
import java.io.File
import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val dateFormat = DateTimeFormatter.ofPattern("dd/MM/yyyy")

internal fun formatDate(iso: String): String =
    runCatching { LocalDate.parse(iso).format(dateFormat) }.getOrDefault(iso)

internal fun statementReference(month: Int, year: Int): String =
    month.toString().padStart(2, '0') + "/" + year

@Composable
internal fun statementStatusLabel(status: StatementStatus): String = stringResource(
    when (status) {
        StatementStatus.Open -> R.string.statement_status_open
        StatementStatus.Closed -> R.string.statement_status_closed
        StatementStatus.Paid -> R.string.statement_status_paid
        StatementStatus.PartiallyPaid -> R.string.statement_status_partial
        StatementStatus.Overdue -> R.string.statement_status_overdue
    },
)

internal fun openStatementPdf(context: Context, pdf: StatementPdf) {
    val dir = File(context.cacheDir, "statements").apply { mkdirs() }
    val safeName = pdf.fileName.replace(Regex("[^A-Za-z0-9._-]"), "_").ifBlank { "fatura.pdf" }
    val file = File(dir, safeName)
    file.writeBytes(pdf.bytes)

    val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
    val intent = Intent(Intent.ACTION_VIEW).apply {
        setDataAndType(uri, pdf.contentType)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    try {
        context.startActivity(Intent.createChooser(intent, pdf.fileName))
    } catch (_: ActivityNotFoundException) {
        val share = Intent(Intent.ACTION_SEND).apply {
            type = pdf.contentType
            putExtra(Intent.EXTRA_STREAM, uri)
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        context.startActivity(Intent.createChooser(share, pdf.fileName))
    }
}
