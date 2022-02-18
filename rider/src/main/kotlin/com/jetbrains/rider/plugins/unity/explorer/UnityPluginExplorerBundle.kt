package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

class UnityPluginExplorerBundle: DynamicBundle(BUNDLE) {
    companion object {
        @NonNls
        private const val BUNDLE = "messages.UnityPluginExplorerBundle"
        private val INSTANCE: UnityPluginExplorerBundle = UnityPluginExplorerBundle()

        @Nls
        fun message(
            @PropertyKey(resourceBundle = BUNDLE) key: String,
            vararg params: Any
        ): String {
            return INSTANCE.getMessage(key, *params)
        }
    }
}