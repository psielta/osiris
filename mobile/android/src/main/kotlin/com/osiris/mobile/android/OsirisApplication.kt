package com.osiris.mobile.android

import android.app.Application
import com.osiris.mobile.android.di.appModule
import com.osiris.mobile.di.sharedModule
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin

class OsirisApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        startKoin {
            androidContext(this@OsirisApplication)
            modules(sharedModule, appModule)
        }
    }
}
