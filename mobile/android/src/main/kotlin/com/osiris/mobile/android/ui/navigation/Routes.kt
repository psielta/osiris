package com.osiris.mobile.android.ui.navigation

object Routes {
    const val Splash = "splash"
    const val Login = "login"
    const val Register = "register"
    const val ForgotPassword = "forgot-password"
    const val Home = "home"
    const val More = "more"
    const val Dashboard = "dashboard"
    const val DocsList = "docs"
    const val DocsDetailsPattern = "docs/{docSlug}"
    const val DocSlugArg = "docSlug"
    const val Reports = "reports"
    const val CategoriesList = "categories"
    const val CategoryForm = "category-form"
    const val CategoryFormPattern = "category-form?categoryId={categoryId}"
    const val CategoryIdArg = "categoryId"

    const val AccountsList = "accounts"
    const val AccountForm = "account-form"
    const val AccountFormPattern = "account-form?accountId={accountId}"
    const val AccountStatementPattern = "account-statement/{accountId}"
    const val MovementFormPattern = "movement-form/{accountId}"
    const val OfxImportPattern = "ofx-import/{accountId}"
    const val CsvImportPattern = "csv-import/{accountId}"
    const val PdfImportPattern = "pdf-import/{accountId}"
    const val AccountIdArg = "accountId"

    const val CardsList = "cards"
    const val CardForm = "card-form"
    const val CardFormPattern = "card-form?cardId={cardId}"
    const val CardDetailsPattern = "card-details/{cardId}"
    const val PurchaseFormPattern = "purchase-form/{cardId}"
    const val PurchaseDetailsPattern = "purchase-details/{cardId}/{purchaseId}"
    const val StatementDetailsPattern = "statement-details/{cardId}/{statementId}"
    const val PaymentFormPattern = "payment-form/{cardId}/{statementId}"
    const val AllPurchases = "all-purchases"
    const val AllStatements = "all-statements"
    const val BillsList = "bills"
    const val BillForm = "bill-form"
    const val BillFormPattern = "bill-form?billId={billId}"
    const val BillDetailsPattern = "bill-details/{billId}"
    const val CardIdArg = "cardId"
    const val PurchaseIdArg = "purchaseId"
    const val StatementIdArg = "statementId"
    const val BillIdArg = "billId"

    fun categoryForm(categoryId: String? = null): String =
        if (categoryId == null) CategoryForm else "category-form?categoryId=$categoryId"

    fun accountForm(accountId: String? = null): String =
        if (accountId == null) AccountForm else "account-form?accountId=$accountId"

    fun accountStatement(accountId: String): String = "account-statement/$accountId"

    fun movementForm(accountId: String): String = "movement-form/$accountId"

    fun ofxImport(accountId: String): String = "ofx-import/$accountId"

    fun csvImport(accountId: String): String = "csv-import/$accountId"

    fun pdfImport(accountId: String): String = "pdf-import/$accountId"

    fun cardForm(cardId: String? = null): String =
        if (cardId == null) CardForm else "card-form?cardId=$cardId"

    fun cardDetails(cardId: String): String = "card-details/$cardId"

    fun purchaseForm(cardId: String): String = "purchase-form/$cardId"

    fun purchaseDetails(cardId: String, purchaseId: String): String = "purchase-details/$cardId/$purchaseId"

    fun statementDetails(cardId: String, statementId: String): String = "statement-details/$cardId/$statementId"

    fun paymentForm(cardId: String, statementId: String): String = "payment-form/$cardId/$statementId"

    fun billForm(billId: String? = null): String =
        if (billId == null) BillForm else "bill-form?billId=$billId"

    fun billDetails(billId: String): String = "bill-details/$billId"

    fun docsDetails(slug: String): String = "docs/$slug"
}
