package com.osiris.mobile.android.feature.cards

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Info
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import com.osiris.mobile.android.R

/**
 * The credit-card area as a single hub with three sub-tabs: Cartões, Faturas and Compras. This surfaces
 * faturas and compras (previously buried under "Mais") one tap away, grouped by domain. A help action in
 * the top bar opens the in-app documentation.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CardsHubScreen(
    onCreate: () -> Unit,
    onEdit: (String) -> Unit,
    onOpenDetails: (String) -> Unit,
    onOpenStatement: (String, String) -> Unit,
    onOpenPurchase: (String, String) -> Unit,
    onNavigateDocs: () -> Unit,
) {
    var selectedTab by rememberSaveable { mutableIntStateOf(0) }
    val snackbarHostState = remember { SnackbarHostState() }
    val tabLabels = listOf(
        R.string.cards_title,
        R.string.card_tab_statements,
        R.string.card_tab_purchases,
    )

    Scaffold(
        topBar = {
            Column {
                TopAppBar(
                    title = { Text(stringResource(R.string.cards_title)) },
                    actions = {
                        IconButton(onClick = onNavigateDocs) {
                            Icon(Icons.Filled.Info, contentDescription = stringResource(R.string.docs_title))
                        }
                    },
                )
                TabRow(selectedTabIndex = selectedTab) {
                    tabLabels.forEachIndexed { index, labelRes ->
                        Tab(
                            selected = selectedTab == index,
                            onClick = { selectedTab = index },
                            text = { Text(stringResource(labelRes)) },
                        )
                    }
                }
            }
        },
        floatingActionButton = {
            if (selectedTab == 0) {
                ExtendedFloatingActionButton(
                    onClick = onCreate,
                    icon = { Icon(Icons.Filled.Add, contentDescription = null) },
                    text = { Text(stringResource(R.string.card_new)) },
                )
            }
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        val contentModifier = Modifier.fillMaxSize().padding(padding)
        when (selectedTab) {
            0 -> CardsListContent(
                modifier = contentModifier,
                snackbarHostState = snackbarHostState,
                onEdit = onEdit,
                onOpenDetails = onOpenDetails,
            )

            1 -> AllStatementsContent(
                modifier = contentModifier,
                onOpenStatement = onOpenStatement,
            )

            else -> AllPurchasesContent(
                modifier = contentModifier,
                onOpenPurchase = onOpenPurchase,
            )
        }
    }
}
