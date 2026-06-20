package com.osiris.mobile.domain.model

/** How the amount of each row is expressed in the CSV. Maps to/from the backend's integer code. */
enum class CsvAmountMode(val apiValue: Int) {
    SignedAmount(1),
    DebitCredit(2),
    TypeColumn(3);

    companion object {
        fun fromApi(value: Int): CsvAmountMode = entries.firstOrNull { it.apiValue == value } ?: SignedAmount
    }
}

/** Column/format mapping the user configures before previewing a CSV statement. */
data class CsvImportMapping(
    val delimiter: String = ";",
    val encoding: String = "utf-8",
    val hasHeader: Boolean = true,
    val headerLineIndex: Int = 0,
    val amountMode: CsvAmountMode = CsvAmountMode.SignedAmount,
    val dateColumn: Int = 0,
    val descriptionColumn: Int = 0,
    val secondaryDescriptionColumn: Int? = null,
    val amountColumn: Int? = null,
    val creditColumn: Int? = null,
    val debitColumn: Int? = null,
    val typeColumn: Int? = null,
    val externalIdColumn: Int? = null,
    val dateFormat: String = "dd/MM/yyyy",
    val decimalSeparator: String = ",",
)

/** Result of asking the backend to sniff a CSV file's structure. */
data class CsvAnalysis(
    val accountId: String,
    val accountName: String,
    val delimiter: String,
    val encoding: String,
    val suggestedHeaderLineIndex: Int,
    val sampleRows: List<List<String>>,
    val savedMapping: CsvImportMapping?,
)
