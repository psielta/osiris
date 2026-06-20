package com.osiris.mobile.android

import android.app.Application
import androidx.lifecycle.DefaultLifecycleObserver
import androidx.lifecycle.LifecycleOwner
import androidx.lifecycle.ProcessLifecycleOwner
import com.osiris.mobile.android.di.appModule
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.di.sharedModule
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin

class OsirisApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        val koinApplication = startKoin {
            androidContext(this@OsirisApplication)
            modules(sharedModule, appModule)
        }

        val dataChangeBus = koinApplication.koin.get<DataChangeBus>()
        ProcessLifecycleOwner.get().lifecycle.addObserver(ForegroundRefreshObserver(dataChangeBus))
    }
}

private class ForegroundRefreshObserver(
    private val dataChangeBus: DataChangeBus,
) : DefaultLifecycleObserver {
    private var isFirstStart = true

    override fun onStart(owner: LifecycleOwner) {
        if (isFirstStart) {
            isFirstStart = false
            return
        }

        dataChangeBus.notify(*DataScope.entries.toTypedArray())
    }
}
