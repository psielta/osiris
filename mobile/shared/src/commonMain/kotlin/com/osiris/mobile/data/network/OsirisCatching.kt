package com.osiris.mobile.data.network

import com.osiris.mobile.core.result.OsirisResult

/**
 * Generic wrapper for a network call: returns [OsirisResult.Success] or maps any failure to an
 * [OsirisResult.Failure] via [NetworkErrorMapper]. (The auth repository's `runAuth` is private and
 * typed for AuthUser, so the rest of the app uses this generic helper.)
 */
suspend fun <T> osirisCatching(block: suspend () -> T): OsirisResult<T> =
    try {
        OsirisResult.Success(block())
    } catch (throwable: Throwable) {
        OsirisResult.Failure(NetworkErrorMapper.map(throwable))
    }
