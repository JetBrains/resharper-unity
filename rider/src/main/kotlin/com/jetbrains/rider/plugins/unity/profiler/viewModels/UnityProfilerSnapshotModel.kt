package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rider.plugins.unity.model.ProfilerSnapshotRequest
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendModelSnapshot

class UnityProfilerSnapshotModel (val profilerModel: FrontendBackendProfilerModel, val lifetime: Lifetime)
{
    val currentSnapshot: Property<FrontendModelSnapshot?> = Property(null)
    val isDataUpToDate : IProperty<Boolean> = Property(false)

    init {
        profilerModel.currentSnapshot.advise(lifetime) { snapshot ->
            currentSnapshot.set(snapshot)
        }

        profilerModel.selectionState.advise(lifetime){ selection ->
            if (selection == null) {
                isDataUpToDate.set(false)
                return@advise
            }

            if(currentSnapshot.value == null){
                isDataUpToDate.set(false)
                return@advise
            }

            if(selection != currentSnapshot.value?.selectionState){
                isDataUpToDate.set(false)
                return@advise
            }

            isDataUpToDate.set(true)
        }
        
        profilerModel.selectionState.adviseNotNull(lifetime) { selectionState ->
            requestNewSnapshot(
                ProfilerSnapshotRequest(
                    selectionState.selectedFrameIndex,
                    selectionState.selectedThread
                )
            )
        }
    }

    fun requestNewSnapshot(profilerSnapshotRequest: ProfilerSnapshotRequest) {
        profilerModel.updateUnityProfilerSnapshotData.fire(
            profilerSnapshotRequest
        )
    }
}