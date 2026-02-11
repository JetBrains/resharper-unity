package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rider.plugins.unity.model.ProfilerSnapshotRequest
import com.jetbrains.rider.plugins.unity.model.ProfilerThread
import com.jetbrains.rider.plugins.unity.model.SelectionState
import com.jetbrains.rider.plugins.unity.model.TimingInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel

class UnityProfilerChartViewModel(
    val profilerModel: FrontendBackendProfilerModel,
    val lifetime: Lifetime
) {
    companion object {
        val Y_STEPS = listOf(1.0, 2.0, 4.0, 8.0, 16.0, 33.0, 66.0, 80.0)
    }

    val frameDurations: IProperty<List<Double>> = Property(emptyList())
    val frameDurationsUpdated: Signal<Unit> = Signal()

    val threadNames: IProperty<List<ProfilerThread>> = Property(emptyList())
    val threadNamesUpdated: Signal<Unit> = Signal()

    val selectedFrameIndex: IProperty<Int?> = Property(null)
    val selectedThread: IProperty<ProfilerThread?> = Property(null)

    val chartYMax: Property<Double> = Property(Y_STEPS.last())
    val startIndex: IProperty<Int> = Property(0)
    val lastIndex: IProperty<Int> = Property(0)
    val fetchingMode: IOptProperty<FetchingMode> get() = profilerModel.fetchingMode

    // Guard flags to prevent circular update loops
    private var isUpdatingFrameSelection = false
    private var isUpdatingThreadSelection = false

    init {
        profilerModel.mainThreadTimingsAndThreads.advise(lifetime) { recordInfo ->

            if (recordInfo == null) {
                clearData()
                return@advise
            }

            recordInfo.samples?.let { updateFrameDurations(it) }
            recordInfo.threads?.let { threads ->
                threadNames.set(threads)
                threadNamesUpdated.fire(Unit)
            }
        }

        profilerModel.currentProfilerRecordInfo.adviseNotNull(lifetime) { recordInfo ->
            startIndex.set(recordInfo.firstFrameId)
            lastIndex.set(recordInfo.lastFrameId)
        }

        profilerModel.selectionState.adviseNotNull(lifetime) { selectionState ->
            if (isUpdatingFrameSelection) return@adviseNotNull
            isUpdatingFrameSelection = true
            try {
                selectedFrameIndex.set(selectionState.selectedFrameIndex)
            } finally {
                isUpdatingFrameSelection = false
            }
        }
        selectedFrameIndex.adviseNotNull(lifetime) {
            if (isUpdatingFrameSelection) return@adviseNotNull
            isUpdatingFrameSelection = true
            try {
                if (it != profilerModel.selectionState.value?.selectedFrameIndex) {
                    var newValue = profilerModel.selectionState.value?.copy(selectedFrameIndex = it)
                    if(newValue == null) {

                        newValue = SelectionState(it, selectedThread.value ?:
                        threadNames.value.firstOrNull() ?:
                        ProfilerThread(0, "Main Thread"))
                    }

                    profilerModel.selectionState.set(newValue)
                }
            } finally {
                isUpdatingFrameSelection = false
            }
        }

        profilerModel.selectionState.adviseNotNull(lifetime) { selectionState ->
            if (isUpdatingThreadSelection) return@adviseNotNull
            isUpdatingThreadSelection = true
            try {
                selectedThread.set(selectionState.selectedThread)
            } finally {
                isUpdatingThreadSelection = false
            }
        }

        selectedThread.adviseNotNull(lifetime) { threadInfo ->
            if (isUpdatingThreadSelection) return@adviseNotNull
            isUpdatingThreadSelection = true
            try {
                if (profilerModel.selectionState.value?.selectedThread != threadInfo) {
                    var newValue = profilerModel.selectionState.value?.copy(selectedThread = threadInfo)
                    if(newValue == null) {
                        newValue = SelectionState(selectedFrameIndex.value ?: startIndex.value, threadInfo)
                    }
                    profilerModel.selectionState.set(newValue)
                }
            } finally {
                isUpdatingThreadSelection = false
            }
        }


        profilerModel.selectionState.adviseNotNull(lifetime) { selectionState ->
            profilerModel.updateUnityProfilerSnapshotData.fire(
                ProfilerSnapshotRequest(
                    selectionState.selectedFrameIndex,
                    selectionState.selectedThread
                )
            )
        }
    }

    private fun updateFrameDurations(timings: List<TimingInfo>) {
        val durations = timings.map { it.ms.toDouble() }
        frameDurations.set(durations)

        val maxDuration = durations.maxOrNull() ?: 0.0
        val newYMax = Y_STEPS.firstOrNull { it >= maxDuration } ?: Y_STEPS.last()
        chartYMax.set(newYMax)
        frameDurationsUpdated.fire(Unit)
    }

    private fun clearData() {
        frameDurations.set(emptyList())
        threadNames.set(emptyList())
        startIndex.set(0)
        lastIndex.set(0)
        selectedFrameIndex.set(null)
        selectedThread.set(null)
        chartYMax.set(Y_STEPS.last())
        frameDurationsUpdated.fire(Unit)
        threadNamesUpdated.fire(Unit)
    }

    fun selectFrame(index: Int?) {
        selectedFrameIndex.set(index)
    }

    fun selectPreviousFrame() {
        val current = selectedFrameIndex.value ?: return
        if (current > startIndex.value) {
            selectedFrameIndex.set(current - 1)
        }
    }

    fun selectNextFrame() {
        val current = selectedFrameIndex.value ?: return
        if (current < lastIndex.value) {
            selectedFrameIndex.set(current + 1)
        }
    }

    fun getFrameDuration(globalIndex: Int): Double? {
        val localIndex = globalIndex - startIndex.value
        val durations = frameDurations.value
        if (localIndex in durations.indices) {
            return durations[localIndex]
        }
        return null
    }
}