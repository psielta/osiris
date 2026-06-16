package com.osiris.mobile.data.session

/**
 * Persists the token pair. The Android implementation uses Jetpack DataStore in app-private storage;
 * hardening with Android Keystore / iOS Keychain encryption is a follow-up behind this same seam.
 */
interface TokenStore {
    suspend fun read(): TokenBundle?
    suspend fun save(bundle: TokenBundle)
    suspend fun clear()
}
