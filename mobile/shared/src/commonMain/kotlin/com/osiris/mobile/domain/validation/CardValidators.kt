package com.osiris.mobile.domain.validation

import com.osiris.mobile.core.format.Money

object CardValidators {
    fun name(value: String): String? = when {
        value.isBlank() -> "Informe o nome do cartao."
        value.length > 100 -> "O nome do cartao deve ter no maximo 100 caracteres."
        else -> null
    }

    fun money(value: String, fieldName: String = "valor"): String? {
        if (value.isBlank()) return "Informe o $fieldName."
        val parsed = Money.parse(value) ?: return "Informe um valor valido."
        return if (parsed < 0.0) "O $fieldName deve ser maior ou igual a zero." else null
    }

    fun positiveMoney(value: String, fieldName: String = "valor"): String? {
        if (value.isBlank()) return "Informe o $fieldName."
        val parsed = Money.parse(value) ?: return "Informe um valor valido."
        return if (parsed <= 0.0) "O $fieldName deve ser maior que zero." else null
    }

    fun day(value: String, label: String): String? {
        val parsed = value.toIntOrNull() ?: return "Informe o $label."
        return if (parsed !in 1..31) "O $label deve estar entre 1 e 31." else null
    }

    fun description(value: String): String? = when {
        value.isBlank() -> "Informe a descricao da compra."
        value.length > 200 -> "A descricao deve ter no maximo 200 caracteres."
        else -> null
    }

    fun category(value: String?): String? =
        if (value.isNullOrBlank()) "Selecione a categoria da compra." else null

    fun installments(value: String): String? {
        val parsed = value.toIntOrNull() ?: return "Informe o numero de parcelas."
        return if (parsed !in 1..120) "O numero de parcelas deve estar entre 1 e 120." else null
    }

    fun notes(value: String): String? =
        if (value.length > 500) "As observacoes devem ter no maximo 500 caracteres." else null
}
