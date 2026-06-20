package com.osiris.mobile.android.di

import com.osiris.mobile.android.BuildConfig
import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.session.DataStoreTokenStore
import com.osiris.mobile.data.session.TokenStore
import com.osiris.mobile.presentation.accounts.AccountFormViewModel
import com.osiris.mobile.presentation.accounts.AccountStatementViewModel
import com.osiris.mobile.presentation.accounts.AccountsListViewModel
import com.osiris.mobile.presentation.accounts.CsvImportViewModel
import com.osiris.mobile.presentation.accounts.MovementFormViewModel
import com.osiris.mobile.presentation.accounts.OfxImportViewModel
import com.osiris.mobile.presentation.bills.BillDetailsViewModel
import com.osiris.mobile.presentation.bills.BillFormViewModel
import com.osiris.mobile.presentation.bills.BillsListViewModel
import com.osiris.mobile.presentation.cards.AllPurchasesViewModel
import com.osiris.mobile.presentation.cards.AllStatementsViewModel
import com.osiris.mobile.presentation.cards.CardDetailsViewModel
import com.osiris.mobile.presentation.cards.CardFormViewModel
import com.osiris.mobile.presentation.cards.CardsListViewModel
import com.osiris.mobile.presentation.cards.PaymentFormViewModel
import com.osiris.mobile.presentation.cards.PurchaseDetailsViewModel
import com.osiris.mobile.presentation.cards.PurchaseFormViewModel
import com.osiris.mobile.presentation.cards.StatementDetailsViewModel
import com.osiris.mobile.presentation.categories.CategoriesListViewModel
import com.osiris.mobile.presentation.categories.CategoryFormViewModel
import com.osiris.mobile.presentation.dashboard.DashboardViewModel
import com.osiris.mobile.presentation.home.HomeViewModel
import com.osiris.mobile.presentation.login.ForgotPasswordViewModel
import com.osiris.mobile.presentation.login.LoginViewModel
import com.osiris.mobile.presentation.register.RegisterViewModel
import com.osiris.mobile.presentation.reports.ReportsViewModel
import com.osiris.mobile.presentation.splash.SplashViewModel
import org.koin.android.ext.koin.androidContext
import org.koin.core.module.dsl.viewModel
import org.koin.core.module.dsl.viewModelOf
import org.koin.dsl.module

val appModule = module {
    single { ApiConfig(BuildConfig.BASE_URL) }
    single<TokenStore> { DataStoreTokenStore(androidContext()) }

    viewModelOf(::LoginViewModel)
    viewModelOf(::ForgotPasswordViewModel)
    viewModelOf(::RegisterViewModel)
    viewModelOf(::HomeViewModel)
    viewModelOf(::SplashViewModel)
    viewModelOf(::CategoriesListViewModel)
    viewModel { parameters -> CategoryFormViewModel(get(), parameters.getOrNull<String>()) }
    viewModelOf(::AccountsListViewModel)
    viewModel { parameters -> AccountFormViewModel(get(), parameters.getOrNull<String>()) }
    viewModel { parameters -> AccountStatementViewModel(get(), get(), parameters.get<String>()) }
    viewModel { parameters -> MovementFormViewModel(get(), get(), parameters.get<String>()) }
    viewModel { parameters -> OfxImportViewModel(get(), get(), parameters.get<String>()) }
    viewModel { parameters -> CsvImportViewModel(get(), get(), parameters.get<String>()) }
    viewModelOf(::CardsListViewModel)
    viewModel { parameters -> CardFormViewModel(get(), get(), parameters.getOrNull<String>()) }
    viewModel { parameters -> CardDetailsViewModel(get(), get(), parameters.get<String>()) }
    viewModel { parameters -> PurchaseFormViewModel(get(), get(), parameters.get<String>()) }
    viewModel { parameters -> PurchaseDetailsViewModel(get(), get(), parameters[0], parameters[1]) }
    viewModel { parameters -> StatementDetailsViewModel(get(), get(), parameters[0], parameters[1]) }
    viewModel { parameters -> PaymentFormViewModel(get(), get(), parameters[0], parameters[1]) }
    viewModelOf(::DashboardViewModel)
    viewModelOf(::BillsListViewModel)
    viewModel { parameters -> BillFormViewModel(get(), get(), get(), parameters.getOrNull<String>()) }
    viewModel { parameters -> BillDetailsViewModel(get(), get(), get(), parameters.get<String>()) }
    viewModelOf(::AllPurchasesViewModel)
    viewModelOf(::AllStatementsViewModel)
    viewModelOf(::ReportsViewModel)
}
