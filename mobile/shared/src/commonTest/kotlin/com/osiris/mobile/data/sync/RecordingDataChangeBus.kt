package com.osiris.mobile.data.sync

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.asSharedFlow

class RecordingDataChangeBus : DataChangeBus {
    private val _changes = MutableSharedFlow<DataScope>(extraBufferCapacity = 64)

    val emitted = mutableListOf<DataScope>()
    override val changes: Flow<DataScope> = _changes.asSharedFlow()

    override fun notify(vararg scopes: DataScope) {
        emitted += scopes
        scopes.forEach { _changes.tryEmit(it) }
    }
}
