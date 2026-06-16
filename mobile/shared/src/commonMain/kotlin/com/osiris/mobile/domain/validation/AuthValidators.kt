package com.osiris.mobile.domain.validation

/** Client-side validation mirroring the server FluentValidation rules (pt-BR messages). */
object AuthValidators {
    fun email(value: String): String? = when {
        value.isBlank() -> "Informe o e-mail."
        !value.contains('@') || !value.substringAfterLast('@').contains('.') -> "Informe um e-mail válido."
        value.length > 256 -> "O e-mail deve ter no máximo 256 caracteres."
        else -> null
    }

    fun password(value: String): String? = when {
        value.isBlank() -> "Informe a senha."
        value.length < 6 -> "A senha deve ter pelo menos 6 caracteres."
        value.length > 100 -> "A senha deve ter no máximo 100 caracteres."
        else -> null
    }

    fun loginPassword(value: String): String? =
        if (value.isBlank()) "Informe a senha." else null

    fun tenantName(value: String): String? = when {
        value.isBlank() -> "Informe o nome da área de trabalho."
        value.length > 100 -> "O nome da área de trabalho deve ter no máximo 100 caracteres."
        else -> null
    }

    fun fullName(value: String): String? = when {
        value.isBlank() -> "Informe seu nome completo."
        value.length > 100 -> "O nome completo deve ter no máximo 100 caracteres."
        else -> null
    }

    fun confirmPassword(password: String, confirm: String): String? = when {
        confirm.isBlank() -> "Confirme a senha."
        confirm != password -> "A confirmação da senha deve ser igual à senha."
        else -> null
    }
}
