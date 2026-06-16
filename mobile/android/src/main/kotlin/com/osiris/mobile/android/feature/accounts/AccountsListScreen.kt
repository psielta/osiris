package com.osiris.mobile.android.feature.accounts

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.RefreshOnResume
import com.osiris.mobile.core.format.Money
import com.osiris.mobile.domain.model.Account
import com.osiris.mobile.domain.model.AccountType
import com.osiris.mobile.presentation.accounts.AccountsListEvent
import com.osiris.mobile.presentation.accounts.AccountsListViewModel
import org.koin.androidx.compose.koinViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountsListScreen(
    onCreate: () -> Unit,
    onEdit: (String) -> Unit,
    onOpenStatement: (String) -> Unit,
    onNavigateBack: () -> Unit,
    viewModel: AccountsListViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var pendingArchive by remember { mutableStateOf<Account?>(null) }

    RefreshOnResume { viewModel.load() }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is AccountsListEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.accounts_title)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
        floatingActionButton = {
            ExtendedFloatingActionButton(
                onClick = onCreate,
                icon = { Icon(Icons.Filled.Add, contentDescription = null) },
                text = { Text(stringResource(R.string.account_new)) },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        when {
            state.isLoading -> Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }

            state.error != null -> Box(Modifier.fillMaxSize().padding(padding).padding(24.dp), Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(state.error!!, color = MaterialTheme.colorScheme.error)
                    Spacer(Modifier.height(12.dp))
                    TextButton(onClick = viewModel::load) { Text(stringResource(R.string.retry)) }
                }
            }

            state.active.isEmpty() && state.archived.isEmpty() -> Box(Modifier.fillMaxSize().padding(padding), Alignment.Center) {
                Text(stringResource(R.string.accounts_empty), color = MaterialTheme.colorScheme.onSurfaceVariant)
            }

            else -> LazyColumn(
                modifier = Modifier.fillMaxSize().padding(padding),
                contentPadding = PaddingValues(16.dp),
            ) {
                if (state.active.isNotEmpty()) {
                    item { SectionHeader(stringResource(R.string.account_active_section)) }
                    items(state.active, key = { it.id }) { account ->
                        AccountRow(account, onClick = { onOpenStatement(account.id) }, onEdit = { onEdit(account.id) }, onArchive = { pendingArchive = account })
                    }
                }
                if (state.archived.isNotEmpty()) {
                    item { SectionHeader(stringResource(R.string.account_archived_section)) }
                    items(state.archived, key = { it.id }) { account ->
                        AccountRow(account, onClick = { onOpenStatement(account.id) }, onEdit = null, onArchive = null)
                    }
                }
            }
        }
    }

    pendingArchive?.let { account ->
        AlertDialog(
            onDismissRequest = { pendingArchive = null },
            text = { Text(stringResource(R.string.account_archive_confirm, account.name)) },
            confirmButton = {
                TextButton(onClick = {
                    viewModel.archive(account.id)
                    pendingArchive = null
                }) { Text(stringResource(R.string.account_archive)) }
            },
            dismissButton = { TextButton(onClick = { pendingArchive = null }) { Text(stringResource(R.string.cancel)) } },
        )
    }
}

@Composable
private fun SectionHeader(text: String) {
    Text(
        text = text,
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(vertical = 8.dp),
    )
}

@Composable
private fun AccountRow(account: Account, onClick: () -> Unit, onEdit: (() -> Unit)?, onArchive: (() -> Unit)?) {
    var menuOpen by remember { mutableStateOf(false) }
    Row(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick).padding(vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(account.name, style = MaterialTheme.typography.bodyLarge)
            Text(
                text = accountTypeLabel(account.type),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text(
            text = Money.brl(account.currentBalance),
            style = MaterialTheme.typography.bodyLarge,
            color = if (account.currentBalance < 0) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurface,
        )
        if (onEdit != null || onArchive != null) {
            Spacer(Modifier.width(4.dp))
            Box {
                IconButton(onClick = { menuOpen = true }) {
                    Icon(Icons.Filled.MoreVert, contentDescription = stringResource(R.string.actions))
                }
                DropdownMenu(expanded = menuOpen, onDismissRequest = { menuOpen = false }) {
                    if (onEdit != null) {
                        DropdownMenuItem(text = { Text(stringResource(R.string.account_edit_action)) }, onClick = {
                            menuOpen = false
                            onEdit()
                        })
                    }
                    if (onArchive != null) {
                        DropdownMenuItem(text = { Text(stringResource(R.string.account_archive)) }, onClick = {
                            menuOpen = false
                            onArchive()
                        })
                    }
                }
            }
        }
    }
}

@Composable
internal fun accountTypeLabel(type: AccountType): String = stringResource(
    when (type) {
        AccountType.Checking -> R.string.account_type_checking
        AccountType.Savings -> R.string.account_type_savings
        AccountType.Cash -> R.string.account_type_cash
        AccountType.Other -> R.string.account_type_other
    },
)
