package com.osiris.mobile.domain.validation

import com.osiris.mobile.core.format.Money

/** Client-side validation mirroring the server FinancialAccount / manual-movement rules (pt-BR). */
object AccountValidators {
    fun name(value: String): String? = when {
        value.isBlank() -> "Informe o nome da conta."
        value.length > 100 -> "O nome da conta deve ter no máximo 100 caracteres."
        else -> null
    }

    fun initialBalance(value: String): String? = when {
        value.isBlank() -> "Informe o saldo inicial."
        Money.parse(value) == null -> "Informe um valor válido."
        else -> null
    }

    fun amount(value: String): String? {
        if (value.isBlank()) return "Informe o valor do lançamento."
        val parsed = Money.parse(value) ?: return "Informe um valor válido."
        return if (parsed <= 0.0) "O valor deve ser maior que zero." else null
    }

    fun description(value: String): String? = when {
        value.isBlank() -> "Informe a descrição do lançamento."
        value.length > 200 -> "A descrição deve ter no máximo 200 caracteres."
        else -> null
    }
}
