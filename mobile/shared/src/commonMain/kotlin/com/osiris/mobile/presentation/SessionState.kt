package com.osiris.mobile.presentation

import com.osiris.mobile.domain.model.AuthUser

sealed interface SessionState {
    data object Loading : SessionState
    data class Authenticated(val user: AuthUser?) : SessionState
    data object Unauthenticated : SessionState
}
