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

    fun categoryForm(categoryId: String? = null): String =
        if (categoryId == null) CategoryForm else "category-form?categoryId=$categoryId"
}
