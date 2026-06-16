package com.osiris.mobile.android.feature.categories

import androidx.compose.foundation.background
import androidx.compose.foundation.border
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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.osiris.mobile.android.R
import com.osiris.mobile.android.ui.components.parseHexColor
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.presentation.categories.CategoriesListEvent
import com.osiris.mobile.presentation.categories.CategoriesListViewModel
import org.koin.androidx.compose.koinViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CategoriesListScreen(
    onCreate: () -> Unit,
    onEdit: (String) -> Unit,
    onNavigateBack: () -> Unit,
    viewModel: CategoriesListViewModel = koinViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var pendingDelete by remember { mutableStateOf<Category?>(null) }
    var pendingArchive by remember { mutableStateOf<Category?>(null) }

    LaunchedEffect(Unit) {
        viewModel.events.collect { event ->
            when (event) {
                is CategoriesListEvent.ShowMessage -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.categories_title)) },
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
                text = { Text(stringResource(R.string.category_new)) },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        when {
            state.isLoading -> Box(
                Modifier.fillMaxSize().padding(padding),
                Alignment.Center,
            ) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }

            state.error != null -> Box(
                Modifier.fillMaxSize().padding(padding).padding(24.dp),
                Alignment.Center,
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(state.error!!, color = MaterialTheme.colorScheme.error)
                    Spacer(Modifier.height(12.dp))
                    TextButton(onClick = viewModel::load) { Text(stringResource(R.string.retry)) }
                }
            }

            state.active.isEmpty() && state.archived.isEmpty() -> Box(
                Modifier.fillMaxSize().padding(padding),
                Alignment.Center,
            ) {
                Text(stringResource(R.string.categories_empty), color = MaterialTheme.colorScheme.onSurfaceVariant)
            }

            else -> LazyColumn(
                modifier = Modifier.fillMaxSize().padding(padding),
                contentPadding = PaddingValues(16.dp),
            ) {
                if (state.active.isNotEmpty()) {
                    item { SectionHeader(stringResource(R.string.category_active_section)) }
                    items(state.active, key = { it.id }) { category ->
                        CategoryRow(
                            category = category,
                            onEdit = { onEdit(category.id) },
                            onArchive = { pendingArchive = category },
                            onDelete = { pendingDelete = category },
                        )
                    }
                }
                if (state.archived.isNotEmpty()) {
                    item { SectionHeader(stringResource(R.string.category_archived_section)) }
                    items(state.archived, key = { it.id }) { category ->
                        CategoryRow(
                            category = category,
                            onEdit = null,
                            onArchive = null,
                            onDelete = { pendingDelete = category },
                        )
                    }
                }
            }
        }
    }

    pendingDelete?.let { category ->
        ConfirmDialog(
            message = stringResource(R.string.category_delete_confirm, category.name),
            confirmLabel = stringResource(R.string.category_delete),
            onConfirm = {
                viewModel.delete(category.id)
                pendingDelete = null
            },
            onDismiss = { pendingDelete = null },
        )
    }
    pendingArchive?.let { category ->
        ConfirmDialog(
            message = stringResource(R.string.category_archive_confirm, category.name),
            confirmLabel = stringResource(R.string.category_archive),
            onConfirm = {
                viewModel.archive(category.id)
                pendingArchive = null
            },
            onDismiss = { pendingArchive = null },
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
private fun CategoryRow(
    category: Category,
    onEdit: (() -> Unit)?,
    onArchive: (() -> Unit)?,
    onDelete: () -> Unit,
) {
    var menuOpen by remember { mutableStateOf(false) }
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .then(if (onEdit != null) Modifier.clickable(onClick = onEdit) else Modifier)
            .padding(vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        val fill = remember(category.color) { category.color?.let { parseHexColor(it) } }
        Box(
            Modifier
                .size(20.dp)
                .clip(CircleShape)
                .background(fill ?: MaterialTheme.colorScheme.surfaceVariant)
                .border(1.dp, MaterialTheme.colorScheme.outlineVariant, CircleShape),
        )
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Text(category.name, style = MaterialTheme.typography.bodyLarge)
            Text(
                text = stringResource(
                    if (category.type == CategoryType.Income) R.string.category_type_income
                    else R.string.category_type_expense,
                ),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Box {
            IconButton(onClick = { menuOpen = true }) {
                Icon(Icons.Filled.MoreVert, contentDescription = stringResource(R.string.actions))
            }
            DropdownMenu(expanded = menuOpen, onDismissRequest = { menuOpen = false }) {
                if (onEdit != null) {
                    DropdownMenuItem(
                        text = { Text(stringResource(R.string.category_edit_action)) },
                        onClick = {
                            menuOpen = false
                            onEdit()
                        },
                    )
                }
                if (onArchive != null) {
                    DropdownMenuItem(
                        text = { Text(stringResource(R.string.category_archive)) },
                        onClick = {
                            menuOpen = false
                            onArchive()
                        },
                    )
                }
                DropdownMenuItem(
                    text = { Text(stringResource(R.string.category_delete)) },
                    onClick = {
                        menuOpen = false
                        onDelete()
                    },
                )
            }
        }
    }
}

@Composable
private fun ConfirmDialog(
    message: String,
    confirmLabel: String,
    onConfirm: () -> Unit,
    onDismiss: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        text = { Text(message) },
        confirmButton = { TextButton(onClick = onConfirm) { Text(confirmLabel) } },
        dismissButton = { TextButton(onClick = onDismiss) { Text(stringResource(R.string.cancel)) } },
    )
}
