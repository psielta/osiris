package com.osiris.mobile.data.remote

import io.ktor.client.engine.HttpClientEngine
import io.ktor.client.engine.okhttp.OkHttp

internal actual fun osirisHttpEngine(): HttpClientEngine = OkHttp.create()
