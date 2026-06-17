package com.osiris.mobile.android.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
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
import com.osiris.mobile.android.feature.home.HomeScreen
import com.osiris.mobile.android.feature.login.ForgotPasswordScreen
import com.osiris.mobile.android.feature.login.LoginScreen
import com.osiris.mobile.android.feature.register.RegisterScreen
import com.osiris.mobile.android.feature.splash.SplashScreen

@Composable
fun OsirisNavHost() {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = Routes.Splash) {
        composable(Routes.Splash) {
            SplashScreen(
                onAuthenticated = {
                    navController.navigate(Routes.Home) {
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
                    navController.navigate(Routes.Home) {
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
                    navController.navigate(Routes.Home) {
                        popUpTo(Routes.Login) { inclusive = true }
                    }
                },
                onNavigateBack = { navController.popBackStack() },
            )
        }
        composable(Routes.Home) {
            HomeScreen(
                onSignedOut = {
                    navController.navigate(Routes.Login) {
                        popUpTo(Routes.Home) { inclusive = true }
                    }
                },
                onNavigateCategories = { navController.navigate(Routes.CategoriesList) },
                onNavigateAccounts = { navController.navigate(Routes.AccountsList) },
                onNavigateCards = { navController.navigate(Routes.CardsList) },
                onNavigateDashboard = { navController.navigate(Routes.Dashboard) },
                onNavigateStatements = { navController.navigate(Routes.AllStatements) },
                onNavigatePurchases = { navController.navigate(Routes.AllPurchases) },
                onNavigateBills = { navController.navigate(Routes.BillsList) },
            )
        }
        composable(Routes.Dashboard) {
            DashboardScreen(onNavigateBack = { navController.popBackStack() })
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
