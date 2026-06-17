package com.osiris.mobile.android.ui.components

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import androidx.core.content.FileProvider
import com.osiris.mobile.domain.model.StatementPdf
import java.io.File

fun openPdf(context: Context, pdf: StatementPdf) {
    val dir = File(context.cacheDir, "statements").apply { mkdirs() }
    val safeName = pdf.fileName.replace(Regex("[^A-Za-z0-9._-]"), "_").ifBlank { "documento.pdf" }
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
