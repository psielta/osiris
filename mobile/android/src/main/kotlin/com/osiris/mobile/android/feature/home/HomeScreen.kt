package com.osiris.mobile.android.feature.home

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisLogo
import com.osiris.mobile.presentation.home.HomeEvent
import com.osiris.mobile.presentation.home.HomeViewModel
import org.koin.androidx.compose.koinViewModel

@Composable
fun HomeScreen(
    onSignedOut: () -> Unit,
    onNavigateCategories: () -> Unit,
    onNavigateAccounts: () -> Unit,
    viewModel: HomeViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                HomeEvent.NavigateLogin -> onSignedOut()
            }
        }
    }

    Surface(color = MaterialTheme.colorScheme.background, modifier = Modifier.fillMaxSize()) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Spacer(Modifier.height(56.dp))
            OsirisLogo(size = 64.dp)
            Spacer(Modifier.height(24.dp))

            if (state.isLoading) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            } else {
                val user = state.user
                Text(
                    text = stringResource(R.string.home_greeting, user?.fullName ?: ""),
                    style = MaterialTheme.typography.headlineSmall,
                )
                Spacer(Modifier.height(8.dp))
                if (user != null) {
                    Text(
                        text = stringResource(R.string.home_workspace, user.tenantName),
                        style = MaterialTheme.typography.bodyLarge,
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = user.email,
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }

            Spacer(Modifier.height(32.dp))
            OutlinedButton(
                onClick = onNavigateAccounts,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(stringResource(R.string.accounts_title))
            }
            Spacer(Modifier.height(12.dp))
            OutlinedButton(
                onClick = onNavigateCategories,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(stringResource(R.string.categories_title))
            }
            Spacer(Modifier.height(12.dp))
            Button(
                onClick = viewModel::signOut,
                enabled = !state.isSigningOut,
                modifier = Modifier.fillMaxWidth(),
            ) {
                if (state.isSigningOut) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(20.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text(stringResource(R.string.home_logout))
                }
            }
        }
    }
}
