package com.jetbrains.rider.plugins.unity.profiler.toolWindow

data class FilterState(val text: String = "", val mode: FilterMatchMode = FilterMatchMode.CONTAINS)

enum class FilterMatchMode {
    /** Exact name match — used by "Filter by" context menu */
    EXACT,
    /** Substring match — used by filter text field */
    CONTAINS,
    /** Matches exact name or names prefixed with "pattern." — used by gutter navigation */
    PREFIX;

    fun matches(name: String, pattern: String): Boolean = when (this) {
        EXACT -> name.equals(pattern, ignoreCase = true)
        CONTAINS -> name.contains(pattern, ignoreCase = true)
        PREFIX -> name.equals(pattern, ignoreCase = true) || name.startsWith("$pattern.", ignoreCase = true)
    }
}
