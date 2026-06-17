package com.osiris.mobile.domain.validation

import com.osiris.mobile.core.format.Money

object BillValidators {
    fun description(value: String): String? = when {
        value.isBlank() -> "Informe a descricao da conta."
        value.length > 200 -> "A descricao deve ter no maximo 200 caracteres."
        else -> null
    }

    fun amount(value: String): String? {
        if (value.isBlank()) return "Informe o valor."
        val parsed = Money.parse(value) ?: return "Informe um valor valido."
        return if (parsed <= 0.0) "O valor deve ser maior que zero." else null
    }

    fun dueDate(value: String): String? =
        if (value.isBlank()) "Informe o vencimento." else null

    fun category(value: String?): String? =
        if (value.isNullOrBlank()) "Selecione a categoria." else null

    fun notes(value: String): String? =
        if (value.length > 500) "As observacoes devem ter no maximo 500 caracteres." else null
}
