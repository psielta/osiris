package com.osiris.mobile.domain.validation

/** Client-side validation mirroring the server FinancialCategory rules (pt-BR messages). */
object CategoryValidators {
    private val hexColor = Regex("^#[0-9A-Fa-f]{6}$")

    fun name(value: String): String? = when {
        value.isBlank() -> "Informe o nome da categoria."
        value.length > 100 -> "O nome da categoria deve ter no máximo 100 caracteres."
        else -> null
    }

    fun color(value: String?): String? = when {
        value.isNullOrBlank() -> null
        !hexColor.matches(value) -> "Escolha uma cor válida."
        else -> null
    }
}
