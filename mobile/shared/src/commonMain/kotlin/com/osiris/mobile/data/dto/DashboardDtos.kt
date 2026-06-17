package com.osiris.mobile.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class DashboardSummaryDto(
    val year: Int,
    val month: Int,
    val onboarding: OnboardingDto,
    val incomeTotal: Double,
    val spendingTotal: Double,
    val spendingByCategory: List<SpendingByCategoryDto> = emptyList(),
    val cashFlow: CashFlowSummaryDto,
    val creditCards: List<CreditCardDashboardDto> = emptyList(),
    val upcomingObligations: List<UpcomingObligationDto> = emptyList(),
    val alerts: List<DashboardAlertDto> = emptyList(),
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

@Serializable
data class OnboardingDto(
    val hasFinancialAccount: Boolean,
    val hasCreditCard: Boolean,
    val hasActiveCategories: Boolean,
    val hasFirstSpending: Boolean,
)

@Serializable
data class SpendingByCategoryDto(
    val categoryId: String? = null,
    val categoryName: String,
    val categoryColor: String? = null,
    val cardPurchasesTotal: Double,
    val billsTotal: Double,
    val directExpensesTotal: Double,
)

@Serializable
data class CashFlowSummaryDto(
    val incomeTotal: Double,
    val billsPaidTotal: Double,
    val statementPaymentsTotal: Double,
    val directExpensesTotal: Double,
    val billsOpenInMonthTotal: Double,
    val statementsOpenInMonthTotal: Double,
    val totalAccountsBalance: Double,
    val projectedCashBalance: Double,
)

@Serializable
data class CreditCardDashboardDto(
    val creditCardId: String,
    val name: String,
    val limit: Double,
    val usedLimit: Double,
    val availableLimit: Double,
    val usagePercentage: Double,
    val currentStatementId: String? = null,
    val currentStatementTotal: Double,
    val currentStatementPaidTotal: Double,
    val currentStatementOpenBalance: Double,
    val currentStatementDueDate: String? = null,
    val currentStatementStatus: Int? = null,
    val futureInstallmentsTotal: Double,
)

@Serializable
data class UpcomingObligationDto(
    val kind: Int,
    val id: String,
    val creditCardId: String? = null,
    val description: String,
    val dueDate: String,
    val amount: Double,
    val isOverdue: Boolean,
)

@Serializable
data class DashboardAlertDto(
    val severity: Int,
    val message: String,
)
