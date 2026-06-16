package com.osiris.mobile.presentation.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.AuthUser
import com.osiris.mobile.domain.repository.AuthRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class HomeUiState(
    val user: AuthUser? = null,
    val isLoading: Boolean = true,
    val isSigningOut: Boolean = false,
)

sealed interface HomeEvent {
    data object NavigateLogin : HomeEvent
}

class HomeViewModel(private val authRepository: AuthRepository) : ViewModel() {

    private val _state = MutableStateFlow(HomeUiState())
    val state: StateFlow<HomeUiState> = _state.asStateFlow()

    private val _events = Channel<HomeEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        load()
    }

    fun load() {
        _state.update { it.copy(isLoading = true) }
        viewModelScope.launch {
            when (val result = authRepository.currentUser()) {
                is OsirisResult.Success -> _state.update { it.copy(user = result.value, isLoading = false) }
                is OsirisResult.Failure -> {
                    // The session could not be resolved (expired/invalid after a failed refresh) — re-auth.
                    _state.update { it.copy(isLoading = false) }
                    _events.send(HomeEvent.NavigateLogin)
                }
            }
        }
    }

    fun signOut() {
        _state.update { it.copy(isSigningOut = true) }
        viewModelScope.launch {
            authRepository.logout()
            _state.update { it.copy(isSigningOut = false) }
            _events.send(HomeEvent.NavigateLogin)
        }
    }
}
