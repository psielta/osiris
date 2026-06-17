package com.osiris.mobile.domain.model

enum class DashboardAlertSeverity(val apiValue: Int) {
    Info(1),
    Warning(2),
    Danger(3);

    companion object {
        fun fromApi(value: Int): DashboardAlertSeverity =
            entries.firstOrNull { it.apiValue == value } ?: Info
    }
}

data class DashboardSummary(
    val year: Int,
    val month: Int,
    val onboarding: Onboarding,
    val incomeTotal: Double,
    val spendingTotal: Double,
    val spendingByCategory: List<SpendingByCategory>,
    val cashFlow: CashFlowSummary,
    val creditCards: List<CreditCardDashboard>,
    val upcomingObligations: List<UpcomingObligation>,
    val alerts: List<DashboardAlert>,
    val billsDueInMonthTotal: Double,
    val billsDueInMonthCount: Int,
    val billsOpenInMonthTotal: Double,
    val statementsDueInMonthTotal: Double,
    val statementsDueInMonthCount: Int,
    val statementsOpenInMonthTotal: Double,
    val totalOpenStatementsBalance: Double,
    val totalOpenBillsBalance: Double,
    val statementPaymentsInMonthTotal: Double,
    val futureInstallmentsTotal: Double,
    val overdueStatementsCount: Int,
    val overdueStatementsBalance: Double,
    val partiallyPaidStatementsCount: Int,
)

data class Onboarding(
    val hasFinancialAccount: Boolean,
    val hasCreditCard: Boolean,
    val hasActiveCategories: Boolean,
    val hasFirstSpending: Boolean,
) {
    val isComplete: Boolean =
        hasFinancialAccount && hasCreditCard && hasActiveCategories && hasFirstSpending
}

data class SpendingByCategory(
    val categoryId: String?,
    val categoryName: String,
    val categoryColor: String?,
    val cardPurchasesTotal: Double,
    val billsTotal: Double,
    val directExpensesTotal: Double,
) {
    val total: Double = cardPurchasesTotal + billsTotal + directExpensesTotal
}

data class CashFlowSummary(
    val incomeTotal: Double,
    val billsPaidTotal: Double,
    val statementPaymentsTotal: Double,
    val directExpensesTotal: Double,
    val billsOpenInMonthTotal: Double,
    val statementsOpenInMonthTotal: Double,
    val totalAccountsBalance: Double,
    val projectedCashBalance: Double,
)

data class CreditCardDashboard(
    val creditCardId: String,
    val name: String,
    val limit: Double,
    val usedLimit: Double,
    val availableLimit: Double,
    val usagePercentage: Double,
    val currentStatementId: String?,
    val currentStatementTotal: Double,
    val currentStatementPaidTotal: Double,
    val currentStatementOpenBalance: Double,
    val currentStatementDueDate: String?,
    val currentStatementStatus: StatementStatus?,
    val futureInstallmentsTotal: Double,
)

data class UpcomingObligation(
    val kind: Int,
    val id: String,
    val creditCardId: String?,
    val description: String,
    val dueDate: String,
    val amount: Double,
    val isOverdue: Boolean,
)

data class DashboardAlert(
    val severity: DashboardAlertSeverity,
    val message: String,
)
