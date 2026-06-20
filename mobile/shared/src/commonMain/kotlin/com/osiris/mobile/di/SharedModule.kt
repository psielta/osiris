package com.osiris.mobile.di

import com.osiris.mobile.data.remote.AccountApi
import com.osiris.mobile.data.remote.AuthApi
import com.osiris.mobile.data.remote.BillApi
import com.osiris.mobile.data.remote.CardApi
import com.osiris.mobile.data.remote.CategoryApi
import com.osiris.mobile.data.remote.DashboardApi
import com.osiris.mobile.data.remote.ReportApi
import com.osiris.mobile.data.remote.buildAuthClient
import com.osiris.mobile.data.remote.buildPlainClient
import com.osiris.mobile.data.repository.AccountRepositoryImpl
import com.osiris.mobile.data.repository.AuthRepositoryImpl
import com.osiris.mobile.data.repository.BillRepositoryImpl
import com.osiris.mobile.data.repository.CardRepositoryImpl
import com.osiris.mobile.data.repository.CategoryRepositoryImpl
import com.osiris.mobile.data.repository.DashboardRepositoryImpl
import com.osiris.mobile.data.repository.ReportRepositoryImpl
import com.osiris.mobile.data.session.SessionManager
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DefaultDataChangeBus
import com.osiris.mobile.domain.repository.AccountRepository
import com.osiris.mobile.domain.repository.AuthRepository
import com.osiris.mobile.domain.repository.BillRepository
import com.osiris.mobile.domain.repository.CardRepository
import com.osiris.mobile.domain.repository.CategoryRepository
import com.osiris.mobile.domain.repository.DashboardRepository
import com.osiris.mobile.domain.repository.ReportRepository
import io.ktor.client.HttpClient
import org.koin.core.qualifier.named
import org.koin.dsl.module

/**
 * Platform-agnostic wiring. [com.osiris.mobile.core.config.ApiConfig] and
 * [com.osiris.mobile.data.session.TokenStore] are provided by the platform app module.
 */
val sharedModule = module {
    single<HttpClient>(named("plain")) { buildPlainClient() }
    single { SessionManager(get(), get(named("plain")), get()) }
    single<HttpClient>(named("auth")) { buildAuthClient(get()) }
    single<DataChangeBus> { DefaultDataChangeBus() }
    single { AuthApi(get(named("plain")), get(named("auth")), get()) }
    single<AuthRepository> { AuthRepositoryImpl(get(), get()) }
    single { CategoryApi(get(named("auth")), get()) }
    single<CategoryRepository> { CategoryRepositoryImpl(get(), get()) }
    single { AccountApi(get(named("auth")), get()) }
    single<AccountRepository> { AccountRepositoryImpl(get(), get()) }
    single { CardApi(get(named("auth")), get()) }
    single<CardRepository> { CardRepositoryImpl(get(), get()) }
    single { BillApi(get(named("auth")), get()) }
    single<BillRepository> { BillRepositoryImpl(get(), get()) }
    single { DashboardApi(get(named("auth")), get()) }
    single<DashboardRepository> { DashboardRepositoryImpl(get()) }
    single { ReportApi(get(named("auth")), get()) }
    single<ReportRepository> { ReportRepositoryImpl(get()) }
}
