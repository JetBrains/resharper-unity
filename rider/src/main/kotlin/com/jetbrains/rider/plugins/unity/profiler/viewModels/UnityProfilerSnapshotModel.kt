package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rider.plugins.unity.model.ProfilerSnapshotRequest
import com.jetbrains.rider.plugins.unity.model.SelectionState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendModelSnapshot

class UnityProfilerSnapshotModel(val profilerModel: FrontendBackendProfilerModel, val lifetime: Lifetime) {
    val currentSnapshot: IProperty<FrontendModelSnapshot?> = Property(null)
    val isDataUpToDate: IProperty<Boolean> = Property(false)

    val fetchingMode: IOptProperty<FetchingMode> get() = profilerModel.fetchingMode
    private val selectionState get() = profilerModel.selectionState

    init {
        profilerModel.currentSnapshot.advise(lifetime) { snapshot ->
            currentSnapshot.set(snapshot)
            updateDataStatus(selectionState.value, currentSnapshot.value)
        }

        selectionState.advise(lifetime) { selection ->
            updateDataStatus(selection, currentSnapshot.value)
        }

        selectionState.adviseNotNull(lifetime) { selectionState ->
            requestNewSnapshot(
                ProfilerSnapshotRequest(
                    selectionState.selectedFrameIndex,
                    selectionState.selectedThread
                )
            )
        }
        
        fetchingMode.advise(lifetime) { mode ->
            if(mode == FetchingMode.Manual) return@advise
            requestNewSnapshot()
        }
    }

    private fun updateDataStatus(
        selection: SelectionState?,
        snapshot: FrontendModelSnapshot?
    ) {
        if (selection == null) {
            isDataUpToDate.set(false)
            return
        }

        if (snapshot == null) {
            isDataUpToDate.set(false)
            return
        }

        if (selection != snapshot?.selectionState) {
            isDataUpToDate.set(false)
            return
        }

        isDataUpToDate.set(true)
    }

    fun requestNewSnapshot() {
        if(selectionState.value == null) return
        requestNewSnapshot(
            ProfilerSnapshotRequest(
                selectionState.value!!.selectedFrameIndex,
                selectionState.value!!.selectedThread
            ),
            true
        )
    }
    private fun requestNewSnapshot(profilerSnapshotRequest: ProfilerSnapshotRequest, force: Boolean = false) {
        if (!force && fetchingMode.valueOrNull == FetchingMode.Manual) return
        launchSnapshotUpdate(profilerSnapshotRequest)
    }

    private fun launchSnapshotUpdate(profilerSnapshotRequest: ProfilerSnapshotRequest) {
        profilerModel.updateUnityProfilerSnapshotData.fire(
            profilerSnapshotRequest
        )
    }
}