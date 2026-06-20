package com.osiris.mobile.data.sync

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.launch

enum class DataScope {
    Accounts,
    Categories,
    Cards,
    Bills,
    Dashboard,
    Reports,
}

interface DataChangeBus {
    val changes: Flow<DataScope>
    fun notify(vararg scopes: DataScope)
}

class DefaultDataChangeBus : DataChangeBus {
    private val _changes = MutableSharedFlow<DataScope>(extraBufferCapacity = 64)
    override val changes: Flow<DataScope> = _changes.asSharedFlow()

    override fun notify(vararg scopes: DataScope) {
        scopes.forEach { _changes.tryEmit(it) }
    }
}

fun ViewModel.observeDataChanges(
    bus: DataChangeBus,
    vararg scopes: DataScope,
    onChange: () -> Unit,
) {
    val observed = scopes.toSet()
    viewModelScope.launch {
        bus.changes.collect { scope ->
            if (scope in observed) {
                onChange()
            }
        }
    }
}
