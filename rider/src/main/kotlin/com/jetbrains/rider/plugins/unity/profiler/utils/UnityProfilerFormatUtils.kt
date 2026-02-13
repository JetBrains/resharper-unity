package com.jetbrains.rider.plugins.unity.profiler.utils

import com.intellij.openapi.util.NlsSafe
import java.text.DecimalFormat
import java.util.Locale
import kotlin.math.log10
import kotlin.math.pow

object UnityProfilerFormatUtils {
    fun formatFileSize(fileSize: Long, unitSeparator: String = " ", rank: Int = -1, fixedFractionPrecision: Boolean = false): String {
        if (fileSize < 0) throw IllegalArgumentException("Invalid value: $fileSize")
        if (fileSize == 0L) return "0${unitSeparator}B"
        var r = rank
        if (r < 0) {
            r = rankForFileSize(fileSize)
        }
        val value = fileSize / 1024.0.pow(r.toDouble())
        val units = arrayOf("B", "KB", "MB", "GB", "TB", "PB", "EB")
        val decimalFormat = DecimalFormat("0.##")
        if (fixedFractionPrecision) {
            decimalFormat.minimumFractionDigits = 2
        }
        return decimalFormat.format(value) + unitSeparator + units[r]
    }

    fun formatMs(ms: Double): String {
        return DecimalFormat("0.00").format(ms)
    }

    fun formatPercentage(percentage: Double): String {
        return DecimalFormat("0.00").format(percentage * 100.0) + "%"
    }

    private fun rankForFileSize(fileSize: Long): Int {
        if (fileSize < 0) throw IllegalArgumentException("Invalid value: $fileSize")
        if (fileSize == 0L) return 0
        return ((log10(fileSize.toDouble()) + 0.0000021214742112756872) / log10(1024.0)).toInt().coerceIn(0, 6)
    }

    fun formatFixedWidthDuration(durationMs: Double): String {
        return if (durationMs >= 100.0) {
            ">100 ms"
        } else {
            formatMs(durationMs)
        }
    }

    @NlsSafe
    fun formatLabel(name: String, durationMs: Double, frameFraction: Double): String {
        val percent = frameFraction * 100.0
        return String.Companion.format(Locale.US, "%s %.2fms (%.1f%%)", name, durationMs, percent)
    }
}