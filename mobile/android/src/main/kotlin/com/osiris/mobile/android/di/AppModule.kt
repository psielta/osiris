package com.osiris.mobile.android.di

import com.osiris.mobile.android.BuildConfig
import com.osiris.mobile.core.config.ApiConfig
import com.osiris.mobile.data.session.DataStoreTokenStore
import com.osiris.mobile.data.session.TokenStore
import com.osiris.mobile.presentation.home.HomeViewModel
import com.osiris.mobile.presentation.login.LoginViewModel
import com.osiris.mobile.presentation.register.RegisterViewModel
import com.osiris.mobile.presentation.splash.SplashViewModel
import org.koin.android.ext.koin.androidContext
import org.koin.core.module.dsl.viewModelOf
import org.koin.dsl.module

val appModule = module {
    single { ApiConfig(BuildConfig.BASE_URL) }
    single<TokenStore> { DataStoreTokenStore(androidContext()) }

    viewModelOf(::LoginViewModel)
    viewModelOf(::RegisterViewModel)
    viewModelOf(::HomeViewModel)
    viewModelOf(::SplashViewModel)
}
