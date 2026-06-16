package com.osiris.mobile.di

import com.osiris.mobile.data.remote.AuthApi
import com.osiris.mobile.data.remote.CategoryApi
import com.osiris.mobile.data.remote.buildAuthClient
import com.osiris.mobile.data.remote.buildPlainClient
import com.osiris.mobile.data.repository.AuthRepositoryImpl
import com.osiris.mobile.data.repository.CategoryRepositoryImpl
import com.osiris.mobile.data.session.SessionManager
import com.osiris.mobile.domain.repository.AuthRepository
import com.osiris.mobile.domain.repository.CategoryRepository
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
    single { AuthApi(get(named("plain")), get(named("auth")), get()) }
    single<AuthRepository> { AuthRepositoryImpl(get(), get()) }
    single { CategoryApi(get(named("auth")), get()) }
    single<CategoryRepository> { CategoryRepositoryImpl(get()) }
}
