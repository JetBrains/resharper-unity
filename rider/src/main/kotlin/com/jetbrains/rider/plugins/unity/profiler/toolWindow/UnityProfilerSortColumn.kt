package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.util.ui.ColumnInfo

enum class UnityProfilerSortColumn(val column: ColumnInfo<*, *>) {
    NAME(UnityProfilerColumns.nameColumn),
    MS(UnityProfilerColumns.msColumn),
    MEMORY(UnityProfilerColumns.memoryColumn),
    FRAME_PERCENTAGE(UnityProfilerColumns.framePercentageColumn)
}
