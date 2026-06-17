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

data class ForgotPasswordUiState(
    val email: String = "",
    val emailError: String? = null,
    val isSubmitting: Boolean = false,
    val requestSent: Boolean = false,
)

sealed interface ForgotPasswordEvent {
    data class ShowMessage(val message: String) : ForgotPasswordEvent
}

class ForgotPasswordViewModel(private val authRepository: AuthRepository) : ViewModel() {

    private val _state = MutableStateFlow(ForgotPasswordUiState())
    val state: StateFlow<ForgotPasswordUiState> = _state.asStateFlow()

    private val _events = Channel<ForgotPasswordEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    fun onEmailChange(value: String) = _state.update {
        it.copy(email = value, emailError = null, requestSent = false)
    }

    fun submit() {
        val current = _state.value
        val email = current.email.trim()
        val emailError = AuthValidators.email(email)
        if (emailError != null) {
            _state.update { it.copy(emailError = emailError) }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            when (val result = authRepository.forgotPassword(email)) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false, requestSent = true) }
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            emailError = error.fieldErrors["email"] ?: it.emailError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(ForgotPasswordEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }
}
