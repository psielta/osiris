package com.osiris.mobile.presentation.splash

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.data.session.SessionManager
import com.osiris.mobile.presentation.SessionState
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

class SplashViewModel(private val sessionManager: SessionManager) : ViewModel() {

    val sessionState: StateFlow<SessionState> = sessionManager.sessionState

    init {
        viewModelScope.launch {
            sessionManager.bootstrap()
        }
    }
}
