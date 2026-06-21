package com.osiris.mobile.android.feature.home

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.Info
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.OsirisLogo
import com.osiris.mobile.presentation.home.HomeEvent
import com.osiris.mobile.presentation.home.HomeViewModel
import org.koin.androidx.compose.koinViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    onSignedOut: () -> Unit,
    onNavigateCategories: () -> Unit,
    onNavigateReports: () -> Unit,
    onNavigateDocs: () -> Unit,
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

    Scaffold(
        topBar = {
            TopAppBar(title = { Text(stringResource(R.string.more_title)) })
        },
    ) { padding ->
        if (state.isLoading) {
            Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }
            return@Scaffold
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
        ) {
            ProfileCard(
                fullName = state.user?.fullName.orEmpty(),
                tenantName = state.user?.tenantName.orEmpty(),
                email = state.user?.email.orEmpty(),
            )

            Spacer(Modifier.height(24.dp))
            Text(
                text = stringResource(R.string.more_secondary_title),
                style = MaterialTheme.typography.titleMedium,
                color = MaterialTheme.colorScheme.primary,
            )
            Spacer(Modifier.height(8.dp))

            MoreActionRow(
                title = stringResource(R.string.reports_title),
                subtitle = stringResource(R.string.more_reports_subtitle),
                icon = Icons.AutoMirrored.Filled.List,
                onClick = onNavigateReports,
            )
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MoreActionRow(
                title = stringResource(R.string.docs_title),
                subtitle = stringResource(R.string.more_docs_subtitle),
                icon = Icons.Filled.Info,
                onClick = onNavigateDocs,
            )
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            MoreActionRow(
                title = stringResource(R.string.categories_title),
                subtitle = stringResource(R.string.more_categories_subtitle),
                icon = Icons.AutoMirrored.Filled.List,
                onClick = onNavigateCategories,
            )

            Spacer(Modifier.height(28.dp))
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

@Composable
private fun ProfileCard(fullName: String, tenantName: String, email: String) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), horizontalAlignment = Alignment.Start) {
            OsirisLogo(size = 48.dp)
            Spacer(Modifier.height(12.dp))
            Text(
                text = stringResource(R.string.home_greeting, fullName),
                style = MaterialTheme.typography.titleMedium,
            )
            Spacer(Modifier.height(4.dp))
            Text(
                text = stringResource(R.string.home_workspace, tenantName),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            if (email.isNotBlank()) {
                Spacer(Modifier.height(2.dp))
                Text(
                    text = email,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}

@Composable
private fun MoreActionRow(
    title: String,
    subtitle: String,
    icon: ImageVector,
    onClick: () -> Unit,
) {
    ListItem(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick),
        leadingContent = { Icon(icon, contentDescription = null) },
        headlineContent = { Text(title) },
        supportingContent = {
            Text(subtitle, color = MaterialTheme.colorScheme.onSurfaceVariant)
        },
    )
}
