package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.util.NlsSafe
import java.util.*

open class ProfilerFormattingUtils {
    companion object {
        fun formatMemory(bytes: Long): String {
            if (bytes < 1024) return "$bytes B"
            if (bytes < 1024 * 1024) return "${bytes / 1024} KB"
            return "${bytes / (1024 * 1024)} MB"
        }

        fun formatFixedWidthDuration(durationMs: Double): String {
            return if (durationMs >= 100.0) {
                ">100 ms"
            } else {
                String.format(Locale.US, "%4.1f ms", durationMs)
            }
        }

        @NlsSafe
        fun formatLabel(name: String, durationMs: Double, frameFraction: Double): String {
            val percent = frameFraction * 100.0
            return String.format(Locale.US, "%s %.2fms (%.1f%%)", name, durationMs, percent)
        }
    }
}