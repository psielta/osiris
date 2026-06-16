package com.osiris.mobile.presentation.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.repository.AuthRepository
import com.osiris.mobile.domain.validation.AuthValidators
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LoginUiState(
    val email: String = "",
    val password: String = "",
    val emailError: String? = null,
    val passwordError: String? = null,
    val isSubmitting: Boolean = false,
)

sealed interface LoginEvent {
    data object NavigateHome : LoginEvent
    data class ShowMessage(val message: String) : LoginEvent
}

class LoginViewModel(private val authRepository: AuthRepository) : ViewModel() {

    private val _state = MutableStateFlow(LoginUiState())
    val state: StateFlow<LoginUiState> = _state.asStateFlow()

    private val _events = Channel<LoginEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    fun onEmailChange(value: String) = _state.update { it.copy(email = value, emailError = null) }

    fun onPasswordChange(value: String) = _state.update { it.copy(password = value, passwordError = null) }

    fun submit() {
        val current = _state.value
        val emailError = AuthValidators.email(current.email.trim())
        val passwordError = AuthValidators.loginPassword(current.password)
        if (emailError != null || passwordError != null) {
            _state.update { it.copy(emailError = emailError, passwordError = passwordError) }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            when (val result = authRepository.login(current.email.trim(), current.password)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(LoginEvent.NavigateHome)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            emailError = error.fieldErrors["email"] ?: it.emailError,
                            passwordError = error.fieldErrors["password"] ?: it.passwordError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(LoginEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }
}
