package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.CashFlowSummaryDto
import com.osiris.mobile.data.dto.CreditCardDashboardDto
import com.osiris.mobile.data.dto.DashboardAlertDto
import com.osiris.mobile.data.dto.DashboardSummaryDto
import com.osiris.mobile.data.dto.OnboardingDto
import com.osiris.mobile.data.dto.SpendingByCategoryDto
import com.osiris.mobile.data.dto.UpcomingObligationDto
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.DashboardApi
import com.osiris.mobile.domain.model.CashFlowSummary
import com.osiris.mobile.domain.model.CreditCardDashboard
import com.osiris.mobile.domain.model.DashboardAlert
import com.osiris.mobile.domain.model.DashboardAlertSeverity
import com.osiris.mobile.domain.model.DashboardSummary
import com.osiris.mobile.domain.model.Onboarding
import com.osiris.mobile.domain.model.SpendingByCategory
import com.osiris.mobile.domain.model.StatementStatus
import com.osiris.mobile.domain.model.UpcomingObligation
import com.osiris.mobile.domain.repository.DashboardRepository

class DashboardRepositoryImpl(private val api: DashboardApi) : DashboardRepository {
    override suspend fun get(month: Int, year: Int): OsirisResult<DashboardSummary> = osirisCatching {
        api.get(month, year).toDomain()
    }
}

private fun DashboardSummaryDto.toDomain() =
    DashboardSummary(
        year,
        month,
        onboarding.toDomain(),
        incomeTotal,
        spendingTotal,
        spendingByCategory.map { it.toDomain() },
        cashFlow.toDomain(),
        creditCards.map { it.toDomain() },
        upcomingObligations.map { it.toDomain() },
        alerts.map { it.toDomain() },
        billsDueInMonthTotal,
        billsDueInMonthCount,
        billsOpenInMonthTotal,
        statementsDueInMonthTotal,
        statementsDueInMonthCount,
        statementsOpenInMonthTotal,
        totalOpenStatementsBalance,
        totalOpenBillsBalance,
        statementPaymentsInMonthTotal,
        futureInstallmentsTotal,
        overdueStatementsCount,
        overdueStatementsBalance,
        partiallyPaidStatementsCount,
    )

private fun OnboardingDto.toDomain() =
    Onboarding(hasFinancialAccount, hasCreditCard, hasActiveCategories, hasFirstSpending)

private fun SpendingByCategoryDto.toDomain() =
    SpendingByCategory(categoryId, categoryName, categoryColor, cardPurchasesTotal, billsTotal, directExpensesTotal)

private fun CashFlowSummaryDto.toDomain() =
    CashFlowSummary(
        incomeTotal,
        billsPaidTotal,
        statementPaymentsTotal,
        directExpensesTotal,
        billsOpenInMonthTotal,
        statementsOpenInMonthTotal,
        totalAccountsBalance,
        projectedCashBalance,
    )

private fun CreditCardDashboardDto.toDomain() =
    CreditCardDashboard(
        creditCardId,
        name,
        limit,
        usedLimit,
        availableLimit,
        usagePercentage,
        currentStatementId,
        currentStatementTotal,
        currentStatementPaidTotal,
        currentStatementOpenBalance,
        currentStatementDueDate,
        currentStatementStatus?.let { StatementStatus.fromApi(it) },
        futureInstallmentsTotal,
    )

private fun UpcomingObligationDto.toDomain() =
    UpcomingObligation(kind, id, creditCardId, description, dueDate, amount, isOverdue)

private fun DashboardAlertDto.toDomain() =
    DashboardAlert(DashboardAlertSeverity.fromApi(severity), message)
