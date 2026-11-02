package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css

import com.intellij.openapi.util.Version

/**
 * Keeps the items that exist in [unityVersion].
 *
 * [unitySince] maps a lower-case item name to the Unity version that introduced it.
 * An item without an entry exists in every Unity version.
 * A `null` [unityVersion] means that the version is unknown, and all items are kept.
 * The comparison ignores the bugfix part, so `6000.3.1` contains an item that is new in `6000.3`.
 */
fun <T> filterByUnityVersion(items: Collection<T>, unitySince: Map<String, Version>, unityVersion: Version?, name: (T) -> String): List<T> {
    if (unityVersion == null) return items.toList()
    val majorMinor = unityVersion.withoutBugfix()
    return items.filter { item ->
        val since = unitySince[name(item)] ?: return@filter true
        since.withoutBugfix() <= majorMinor
    }
}
