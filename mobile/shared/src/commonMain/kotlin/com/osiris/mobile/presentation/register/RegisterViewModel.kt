package com.osiris.mobile.presentation.register

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

data class RegisterUiState(
    val tenantName: String = "",
    val fullName: String = "",
    val email: String = "",
    val password: String = "",
    val confirmPassword: String = "",
    val tenantNameError: String? = null,
    val fullNameError: String? = null,
    val emailError: String? = null,
    val passwordError: String? = null,
    val confirmPasswordError: String? = null,
    val isSubmitting: Boolean = false,
)

sealed interface RegisterEvent {
    data object NavigateHome : RegisterEvent
    data class ShowMessage(val message: String) : RegisterEvent
}

class RegisterViewModel(private val authRepository: AuthRepository) : ViewModel() {

    private val _state = MutableStateFlow(RegisterUiState())
    val state: StateFlow<RegisterUiState> = _state.asStateFlow()

    private val _events = Channel<RegisterEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    fun onTenantNameChange(value: String) = _state.update { it.copy(tenantName = value, tenantNameError = null) }
    fun onFullNameChange(value: String) = _state.update { it.copy(fullName = value, fullNameError = null) }
    fun onEmailChange(value: String) = _state.update { it.copy(email = value, emailError = null) }
    fun onPasswordChange(value: String) = _state.update { it.copy(password = value, passwordError = null) }
    fun onConfirmPasswordChange(value: String) = _state.update { it.copy(confirmPassword = value, confirmPasswordError = null) }

    fun submit() {
        val current = _state.value
        val tenantNameError = AuthValidators.tenantName(current.tenantName.trim())
        val fullNameError = AuthValidators.fullName(current.fullName.trim())
        val emailError = AuthValidators.email(current.email.trim())
        val passwordError = AuthValidators.password(current.password)
        val confirmPasswordError = AuthValidators.confirmPassword(current.password, current.confirmPassword)

        if (tenantNameError != null || fullNameError != null || emailError != null ||
            passwordError != null || confirmPasswordError != null
        ) {
            _state.update {
                it.copy(
                    tenantNameError = tenantNameError,
                    fullNameError = fullNameError,
                    emailError = emailError,
                    passwordError = passwordError,
                    confirmPasswordError = confirmPasswordError,
                )
            }
            return
        }

        _state.update { it.copy(isSubmitting = true) }
        viewModelScope.launch {
            val result = authRepository.register(
                tenantName = current.tenantName.trim(),
                fullName = current.fullName.trim(),
                email = current.email.trim(),
                password = current.password,
                confirmPassword = current.confirmPassword,
            )
            when (result) {
                is OsirisResult.Success -> {
                    _state.update { it.copy(isSubmitting = false) }
                    _events.send(RegisterEvent.NavigateHome)
                }

                is OsirisResult.Failure -> {
                    val error = result.error
                    _state.update {
                        it.copy(
                            isSubmitting = false,
                            tenantNameError = error.fieldErrors["tenantName"] ?: it.tenantNameError,
                            fullNameError = error.fieldErrors["fullName"] ?: it.fullNameError,
                            emailError = error.fieldErrors["email"] ?: it.emailError,
                            passwordError = error.fieldErrors["password"] ?: it.passwordError,
                            confirmPasswordError = error.fieldErrors["confirmPassword"] ?: it.confirmPasswordError,
                        )
                    }
                    if (error.fieldErrors.isEmpty()) {
                        _events.send(RegisterEvent.ShowMessage(error.message))
                    }
                }
            }
        }
    }
}
