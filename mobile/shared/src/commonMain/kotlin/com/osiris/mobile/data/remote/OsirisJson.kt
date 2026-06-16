package com.osiris.mobile.data.remote

import kotlinx.serialization.json.Json

internal val osirisJson: Json = Json {
    ignoreUnknownKeys = true
    isLenient = true
}
