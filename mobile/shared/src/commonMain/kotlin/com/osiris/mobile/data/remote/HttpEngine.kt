package com.osiris.mobile.data.remote

import io.ktor.client.engine.HttpClientEngine

/** Each platform supplies its Ktor engine (OkHttp on Android, Darwin on iOS later). */
internal expect fun osirisHttpEngine(): HttpClientEngine
