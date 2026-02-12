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
    private val isIntegrationEnable: IOptProperty<Boolean> get() = profilerModel.isIntegrationEnable
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
            if (mode == FetchingMode.Manual) return@advise
            requestNewSnapshot()
        }
        
        isIntegrationEnable.advise(lifetime) { isEnable ->
            if (!isEnable) return@advise
            requestNewSnapshot()
        }
    }

    private fun updateDataStatus(
        selection: SelectionState?,
        snapshot: FrontendModelSnapshot?
    ) {
        isDataUpToDate.set(
            selection != null && snapshot != null && selection == snapshot.selectionState
        )
    }

    fun requestNewSnapshot() {
        val selection = selectionState.value ?: return
        requestNewSnapshot(
            ProfilerSnapshotRequest(
                selection.selectedFrameIndex,
                selection.selectedThread
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