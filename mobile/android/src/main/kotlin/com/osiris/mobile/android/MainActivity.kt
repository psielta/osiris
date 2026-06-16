package com.osiris.mobile.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.osiris.mobile.android.ui.navigation.OsirisNavHost
import com.osiris.mobile.android.ui.theme.OsirisTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            OsirisTheme {
                OsirisNavHost()
            }
        }
    }
}
