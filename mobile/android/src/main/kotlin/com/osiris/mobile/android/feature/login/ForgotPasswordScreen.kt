package com.osiris.mobile.android.feature.login

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisLogo
import com.osiris.mobile.android.ui.components.OsirisTextField
import com.osiris.mobile.presentation.login.ForgotPasswordEvent
import com.osiris.mobile.presentation.login.ForgotPasswordViewModel
import org.koin.androidx.compose.koinViewModel

@Composable
fun ForgotPasswordScreen(
    onNavigateBack: () -> Unit,
    viewModel: ForgotPasswordViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is ForgotPasswordEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    Scaffold(snackbarHost = { SnackbarHost(snackbarHostState) }) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 24.dp)
                .verticalScroll(rememberScrollState()),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Spacer(Modifier.height(40.dp))
            OsirisLogo(size = 64.dp)
            Spacer(Modifier.height(16.dp))
            Text(stringResource(R.string.forgot_password_title), style = MaterialTheme.typography.headlineMedium)
            Text(
                text = stringResource(R.string.forgot_password_subtitle),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(Modifier.height(24.dp))

            OsirisTextField(
                value = state.email,
                onValueChange = viewModel::onEmailChange,
                label = stringResource(R.string.forgot_password_email_label),
                error = state.emailError,
                keyboardType = KeyboardType.Email,
                imeAction = ImeAction.Done,
            )

            if (state.requestSent) {
                Spacer(Modifier.height(12.dp))
                Text(
                    text = stringResource(R.string.forgot_password_success),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            Spacer(Modifier.height(24.dp))
            Button(
                onClick = viewModel::submit,
                enabled = !state.isSubmitting,
                modifier = Modifier.fillMaxWidth(),
            ) {
                if (state.isSubmitting) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(20.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text(stringResource(R.string.forgot_password_submit))
                }
            }
            Spacer(Modifier.height(4.dp))
            TextButton(onClick = onNavigateBack, modifier = Modifier.fillMaxWidth()) {
                Text(stringResource(R.string.forgot_password_to_login))
            }
            Spacer(Modifier.height(24.dp))
        }
    }
}
