package com.osiris.mobile.data.sync

import kotlinx.coroutines.flow.take
import kotlinx.coroutines.flow.toList
import kotlinx.coroutines.launch
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals

@OptIn(ExperimentalCoroutinesApi::class)
class DataChangeBusTest {
    @Test
    fun notify_emits_each_scope() = runTest {
        val bus = DefaultDataChangeBus()
        val emitted = mutableListOf<DataScope>()
        val job = launch(UnconfinedTestDispatcher(testScheduler)) {
            bus.changes.take(2).toList(emitted)
        }

        bus.notify(DataScope.Accounts, DataScope.Dashboard)
        job.join()

        assertEquals(listOf(DataScope.Accounts, DataScope.Dashboard), emitted)
    }
}
