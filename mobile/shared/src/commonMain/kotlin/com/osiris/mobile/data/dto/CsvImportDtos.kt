package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class CsvImportMappingDto(
    val delimiter: String = ";",
    val encoding: String = "utf-8",
    val hasHeader: Boolean = true,
    val headerLineIndex: Int = 0,
    val amountMode: Int = 1,
    val dateColumn: Int = 0,
    val descriptionColumn: Int = 0,
    val secondaryDescriptionColumn: Int? = null,
    val amountColumn: Int? = null,
    val creditColumn: Int? = null,
    val debitColumn: Int? = null,
    val typeColumn: Int? = null,
    val externalIdColumn: Int? = null,
    val incomeTokens: List<String> = emptyList(),
    val expenseTokens: List<String> = emptyList(),
    val dateFormat: String = "dd/MM/yyyy",
    val decimalSeparator: String = ",",
)

@Serializable
data class CsvAnalysisDto(
    val accountId: String,
    val accountName: String,
    val delimiter: String = ";",
    val encoding: String = "utf-8",
    val suggestedHeaderLineIndex: Int = 0,
    val sampleRows: List<List<String>> = emptyList(),
    val savedMapping: CsvImportMappingDto? = null,
)

@Serializable
data class PreviewCsvImportRequest(
    val fileName: String,
    val content: String,
    val mapping: CsvImportMappingDto,
)
