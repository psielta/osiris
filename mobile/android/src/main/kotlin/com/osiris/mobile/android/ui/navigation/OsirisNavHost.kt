package com.osiris.mobile.android.ui.navigation

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.osiris.mobile.android.R
import com.osiris.mobile.android.feature.accounts.AccountFormScreen
import com.osiris.mobile.android.feature.accounts.AccountStatementScreen
import com.osiris.mobile.android.feature.accounts.AccountsListScreen
import com.osiris.mobile.android.feature.accounts.MovementFormScreen
import com.osiris.mobile.android.feature.bills.BillDetailsScreen
import com.osiris.mobile.android.feature.bills.BillFormScreen
import com.osiris.mobile.android.feature.bills.BillsListScreen
import com.osiris.mobile.android.feature.cards.AllPurchasesScreen
import com.osiris.mobile.android.feature.cards.AllStatementsScreen
import com.osiris.mobile.android.feature.cards.CardDetailsScreen
import com.osiris.mobile.android.feature.cards.CardFormScreen
import com.osiris.mobile.android.feature.cards.CardsListScreen
import com.osiris.mobile.android.feature.cards.PaymentFormScreen
import com.osiris.mobile.android.feature.cards.PurchaseDetailsScreen
import com.osiris.mobile.android.feature.cards.PurchaseFormScreen
import com.osiris.mobile.android.feature.cards.StatementDetailsScreen
import com.osiris.mobile.android.feature.categories.CategoriesListScreen
import com.osiris.mobile.android.feature.categories.CategoryFormScreen
import com.osiris.mobile.android.feature.dashboard.DashboardScreen
import com.osiris.mobile.android.feature.docs.DocumentationDetailScreen
import com.osiris.mobile.android.feature.docs.DocumentationListScreen
import com.osiris.mobile.android.feature.home.HomeScreen
import com.osiris.mobile.android.feature.login.ForgotPasswordScreen
import com.osiris.mobile.android.feature.login.LoginScreen
import com.osiris.mobile.android.feature.register.RegisterScreen
import com.osiris.mobile.android.feature.reports.ReportsScreen
import com.osiris.mobile.android.feature.splash.SplashScreen

@Composable
fun OsirisNavHost() {
    val navController = rememberNavController()
    val mainTabs = remember {
        listOf(
            MainTab(Routes.Dashboard, R.string.dashboard_title, Icons.Filled.Home),
            MainTab(Routes.AccountsList, R.string.accounts_title, Icons.AutoMirrored.Filled.List),
            MainTab(Routes.CardsList, R.string.cards_title, Icons.Filled.ShoppingCart),
            MainTab(Routes.BillsList, R.string.bills_tab_title, Icons.Filled.Check),
            MainTab(Routes.More, R.string.more_title, Icons.Filled.MoreVert),
        )
    }
    val mainTabRoutes = remember(mainTabs) { mainTabs.map { it.route }.toSet() }
    val backStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = backStackEntry?.destination?.route

    fun navigateMainTab(route: String) {
        navController.navigate(route) {
            popUpTo(Routes.Dashboard) {
                saveState = true
            }
            launchSingleTop = true
            restoreState = true
        }
    }

    Scaffold(
        bottomBar = {
            if (currentRoute in mainTabRoutes) {
                MainBottomBar(
                    tabs = mainTabs,
                    currentRoute = currentRoute,
                    onTabSelected = ::navigateMainTab,
                )
            }
        },
    ) { rootPadding ->
        NavHost(
            navController = navController,
            startDestination = Routes.Splash,
            modifier = Modifier.padding(rootPadding),
        ) {
            composable(Routes.Splash) {
                SplashScreen(
                    onAuthenticated = {
                        navController.navigate(Routes.Dashboard) {
                            popUpTo(Routes.Splash) { inclusive = true }
                        }
                    },
                    onUnauthenticated = {
                        navController.navigate(Routes.Login) {
                            popUpTo(Routes.Splash) { inclusive = true }
                        }
                    },
                )
            }
            composable(Routes.Login) {
                LoginScreen(
                    onNavigateHome = {
                        navController.navigate(Routes.Dashboard) {
                            popUpTo(Routes.Login) { inclusive = true }
                        }
                    },
                    onNavigateRegister = { navController.navigate(Routes.Register) },
                    onNavigateForgotPassword = { navController.navigate(Routes.ForgotPassword) },
                )
            }
            composable(Routes.ForgotPassword) {
                ForgotPasswordScreen(onNavigateBack = { navController.popBackStack() })
            }
            composable(Routes.Register) {
                RegisterScreen(
                    onNavigateHome = {
                        navController.navigate(Routes.Dashboard) {
                            popUpTo(Routes.Login) { inclusive = true }
                        }
                    },
                    onNavigateBack = { navController.popBackStack() },
                )
            }
            composable(Routes.More) {
                HomeScreen(
                    onSignedOut = {
                        navController.navigate(Routes.Login) {
                            popUpTo(Routes.Dashboard) { inclusive = true }
                        }
                    },
                    onNavigateCategories = { navController.navigate(Routes.CategoriesList) },
                    onNavigateStatements = { navController.navigate(Routes.AllStatements) },
                    onNavigatePurchases = { navController.navigate(Routes.AllPurchases) },
                    onNavigateReports = { navController.navigate(Routes.Reports) },
                    onNavigateDocs = { navController.navigate(Routes.DocsList) },
                )
            }
            composable(Routes.Dashboard) {
                DashboardScreen(onNavigateBack = { navController.popBackStack() }, showBackButton = false)
            }
        composable(Routes.DocsList) {
            DocumentationListScreen(
                onNavigateBack = { navController.popBackStack() },
                onOpenGuide = { slug -> navController.navigate(Routes.docsDetails(slug)) },
            )
        }
        composable(
            route = Routes.DocsDetailsPattern,
            arguments = listOf(navArgument(Routes.DocSlugArg) { type = NavType.StringType }),
        ) { backStackEntry ->
            DocumentationDetailScreen(
                slug = backStackEntry.arguments?.getString(Routes.DocSlugArg).orEmpty(),
                onNavigateBack = { navController.popBackStack() },
            )
        }
        composable(Routes.Reports) {
            ReportsScreen(onNavigateBack = { navController.popBackStack() })
        }
        composable(Routes.CategoriesList) {
            CategoriesListScreen(
                onCreate = { navController.navigate(Routes.categoryForm()) },
                onEdit = { id -> navController.navigate(Routes.categoryForm(id)) },
                onNavigateBack = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.CategoryFormPattern,
            arguments = listOf(
                navArgument(Routes.CategoryIdArg) {
                    type = NavType.StringType
                    nullable = true
                    defaultValue = null
                },
            ),
        ) { backStackEntry ->
            CategoryFormScreen(
                categoryId = backStackEntry.arguments?.getString(Routes.CategoryIdArg),
                onDone = { navController.popBackStack() },
            )
        }
        composable(Routes.AccountsList) {
            AccountsListScreen(
                onCreate = { navController.navigate(Routes.accountForm()) },
                onEdit = { id -> navController.navigate(Routes.accountForm(id)) },
                onOpenStatement = { id -> navController.navigate(Routes.accountStatement(id)) },
                onNavigateBack = { navController.popBackStack() },
                showBackButton = false,
            )
        }
        composable(
            route = Routes.AccountFormPattern,
            arguments = listOf(
                navArgument(Routes.AccountIdArg) {
                    type = NavType.StringType
                    nullable = true
                    defaultValue = null
                },
            ),
        ) { backStackEntry ->
            AccountFormScreen(
                accountId = backStackEntry.arguments?.getString(Routes.AccountIdArg),
                onDone = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.AccountStatementPattern,
            arguments = listOf(navArgument(Routes.AccountIdArg) { type = NavType.StringType }),
        ) { backStackEntry ->
            val accountId = backStackEntry.arguments?.getString(Routes.AccountIdArg).orEmpty()
            AccountStatementScreen(
                accountId = accountId,
                onAddMovement = { navController.navigate(Routes.movementForm(accountId)) },
                onNavigateBack = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.MovementFormPattern,
            arguments = listOf(navArgument(Routes.AccountIdArg) { type = NavType.StringType }),
        ) { backStackEntry ->
            MovementFormScreen(
                accountId = backStackEntry.arguments?.getString(Routes.AccountIdArg).orEmpty(),
                onDone = { navController.popBackStack() },
            )
        }
        composable(Routes.CardsList) {
            CardsListScreen(
                onCreate = { navController.navigate(Routes.cardForm()) },
                onEdit = { id -> navController.navigate(Routes.cardForm(id)) },
                onOpenDetails = { id -> navController.navigate(Routes.cardDetails(id)) },
                onNavigateBack = { navController.popBackStack() },
                showBackButton = false,
            )
        }
        composable(Routes.AllPurchases) {
            AllPurchasesScreen(
                onNavigateBack = { navController.popBackStack() },
                onOpenPurchase = { cardId, purchaseId -> navController.navigate(Routes.purchaseDetails(cardId, purchaseId)) },
            )
        }
        composable(Routes.AllStatements) {
            AllStatementsScreen(
                onNavigateBack = { navController.popBackStack() },
                onOpenStatement = { cardId, statementId -> navController.navigate(Routes.statementDetails(cardId, statementId)) },
            )
        }
        composable(Routes.BillsList) {
            BillsListScreen(
                onCreate = { navController.navigate(Routes.billForm()) },
                onOpenDetails = { id -> navController.navigate(Routes.billDetails(id)) },
                onNavigateBack = { navController.popBackStack() },
                showBackButton = false,
            )
        }
        composable(
            route = Routes.BillFormPattern,
            arguments = listOf(
                navArgument(Routes.BillIdArg) {
                    type = NavType.StringType
                    nullable = true
                    defaultValue = null
                },
            ),
        ) { backStackEntry ->
            BillFormScreen(
                billId = backStackEntry.arguments?.getString(Routes.BillIdArg),
                onDone = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.BillDetailsPattern,
            arguments = listOf(navArgument(Routes.BillIdArg) { type = NavType.StringType }),
        ) { backStackEntry ->
            val billId = backStackEntry.arguments?.getString(Routes.BillIdArg).orEmpty()
            BillDetailsScreen(
                billId = billId,
                onNavigateBack = { navController.popBackStack() },
                onEdit = { navController.navigate(Routes.billForm(billId)) },
            )
        }
        composable(
            route = Routes.CardFormPattern,
            arguments = listOf(
                navArgument(Routes.CardIdArg) {
                    type = NavType.StringType
                    nullable = true
                    defaultValue = null
                },
            ),
        ) { backStackEntry ->
            CardFormScreen(
                cardId = backStackEntry.arguments?.getString(Routes.CardIdArg),
                onDone = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.CardDetailsPattern,
            arguments = listOf(navArgument(Routes.CardIdArg) { type = NavType.StringType }),
        ) { backStackEntry ->
            val cardId = backStackEntry.arguments?.getString(Routes.CardIdArg).orEmpty()
            CardDetailsScreen(
                cardId = cardId,
                onNavigateBack = { navController.popBackStack() },
                onEdit = { navController.navigate(Routes.cardForm(cardId)) },
                onAddPurchase = { navController.navigate(Routes.purchaseForm(cardId)) },
                onOpenPurchase = { purchaseId -> navController.navigate(Routes.purchaseDetails(cardId, purchaseId)) },
                onOpenStatement = { statementId -> navController.navigate(Routes.statementDetails(cardId, statementId)) },
            )
        }
        composable(
            route = Routes.PurchaseFormPattern,
            arguments = listOf(navArgument(Routes.CardIdArg) { type = NavType.StringType }),
        ) { backStackEntry ->
            PurchaseFormScreen(
                cardId = backStackEntry.arguments?.getString(Routes.CardIdArg).orEmpty(),
                onDone = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.PurchaseDetailsPattern,
            arguments = listOf(
                navArgument(Routes.CardIdArg) { type = NavType.StringType },
                navArgument(Routes.PurchaseIdArg) { type = NavType.StringType },
            ),
        ) { backStackEntry ->
            PurchaseDetailsScreen(
                cardId = backStackEntry.arguments?.getString(Routes.CardIdArg).orEmpty(),
                purchaseId = backStackEntry.arguments?.getString(Routes.PurchaseIdArg).orEmpty(),
                onNavigateBack = { navController.popBackStack() },
            )
        }
        composable(
            route = Routes.StatementDetailsPattern,
            arguments = listOf(
                navArgument(Routes.CardIdArg) { type = NavType.StringType },
                navArgument(Routes.StatementIdArg) { type = NavType.StringType },
            ),
        ) { backStackEntry ->
            val cardId = backStackEntry.arguments?.getString(Routes.CardIdArg).orEmpty()
            val statementId = backStackEntry.arguments?.getString(Routes.StatementIdArg).orEmpty()
            StatementDetailsScreen(
                cardId = cardId,
                statementId = statementId,
                onNavigateBack = { navController.popBackStack() },
                onPay = { navController.navigate(Routes.paymentForm(cardId, statementId)) },
            )
        }
        composable(
            route = Routes.PaymentFormPattern,
            arguments = listOf(
                navArgument(Routes.CardIdArg) { type = NavType.StringType },
                navArgument(Routes.StatementIdArg) { type = NavType.StringType },
            ),
        ) { backStackEntry ->
            PaymentFormScreen(
                cardId = backStackEntry.arguments?.getString(Routes.CardIdArg).orEmpty(),
                statementId = backStackEntry.arguments?.getString(Routes.StatementIdArg).orEmpty(),
                onDone = { navController.popBackStack() },
            )
        }
    }
}
}

private data class MainTab(
    val route: String,
    val labelRes: Int,
    val icon: ImageVector,
)

@Composable
private fun MainBottomBar(
    tabs: List<MainTab>,
    currentRoute: String?,
    onTabSelected: (String) -> Unit,
) {
    NavigationBar {
        tabs.forEach { tab ->
            NavigationBarItem(
                selected = currentRoute == tab.route,
                onClick = { onTabSelected(tab.route) },
                icon = { Icon(tab.icon, contentDescription = null) },
                label = { Text(androidx.compose.ui.res.stringResource(tab.labelRes)) },
            )
        }
    }
}
