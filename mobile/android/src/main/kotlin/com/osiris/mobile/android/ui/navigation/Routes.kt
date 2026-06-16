package com.osiris.mobile.android.ui.navigation

object Routes {
    const val Splash = "splash"
    const val Login = "login"
    const val Register = "register"
    const val Home = "home"
    const val CategoriesList = "categories"
    const val CategoryForm = "category-form"
    const val CategoryFormPattern = "category-form?categoryId={categoryId}"
    const val CategoryIdArg = "categoryId"

    const val AccountsList = "accounts"
    const val AccountForm = "account-form"
    const val AccountFormPattern = "account-form?accountId={accountId}"
    const val AccountStatementPattern = "account-statement/{accountId}"
    const val MovementFormPattern = "movement-form/{accountId}"
    const val AccountIdArg = "accountId"

    fun categoryForm(categoryId: String? = null): String =
        if (categoryId == null) CategoryForm else "category-form?categoryId=$categoryId"

    fun accountForm(accountId: String? = null): String =
        if (accountId == null) AccountForm else "account-form?accountId=$accountId"

    fun accountStatement(accountId: String): String = "account-statement/$accountId"

    fun movementForm(accountId: String): String = "movement-form/$accountId"
}
