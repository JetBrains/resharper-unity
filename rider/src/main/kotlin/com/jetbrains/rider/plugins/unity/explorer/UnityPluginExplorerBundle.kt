package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

object UnityPluginExplorerBundle {
  @NonNls
  private const val BUNDLE = "messages.UnityPluginExplorerBundle"
  private val instance = DynamicBundle(UnityPluginExplorerBundle::class.java, BUNDLE)

  @Nls
  fun message(
    @PropertyKey(resourceBundle = BUNDLE) key: String,
    vararg params: Any
  ): String {
    return instance.getMessage(key, *params)
  }
}