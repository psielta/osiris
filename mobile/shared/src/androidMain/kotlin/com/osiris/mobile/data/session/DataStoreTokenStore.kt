package com.osiris.mobile.data.session

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first

private val Context.osirisDataStore: DataStore<Preferences> by preferencesDataStore(name = "osiris_session")

/**
 * Stores the token pair in app-private Jetpack DataStore. Encryption at rest (Android Keystore) is a
 * planned follow-up; decrypt/read failure is treated as "no session" so the app falls back to login.
 */
class DataStoreTokenStore(private val context: Context) : TokenStore {

    private val accessKey = stringPreferencesKey("access_token")
    private val refreshKey = stringPreferencesKey("refresh_token")

    override suspend fun read(): TokenBundle? {
        val preferences = context.osirisDataStore.data.first()
        val access = preferences[accessKey]
        val refresh = preferences[refreshKey]
        return if (access != null && refresh != null) TokenBundle(access, refresh) else null
    }

    override suspend fun save(bundle: TokenBundle) {
        context.osirisDataStore.edit { preferences ->
            preferences[accessKey] = bundle.accessToken
            preferences[refreshKey] = bundle.refreshToken
        }
    }

    override suspend fun clear() {
        context.osirisDataStore.edit { it.clear() }
    }
}
