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
import com.osiris.mobile.android.feature.categories.CategoriesListScreen
import com.osiris.mobile.android.feature.categories.CategoryFormScreen
import com.osiris.mobile.android.feature.home.HomeScreen
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
            )
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
            )
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
    }
}
