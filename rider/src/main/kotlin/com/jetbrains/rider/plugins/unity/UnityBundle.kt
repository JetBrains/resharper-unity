package com.jetbrains.rider.plugins.unity

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

object UnityBundle {
  @NonNls
  private const val BUNDLE = "messages.UnityBundle"
  private val instance = DynamicBundle(UnityBundle::class.java, BUNDLE)

  @Nls
  fun message(
    @PropertyKey(resourceBundle = BUNDLE) key: String,
    vararg params: Any
  ): String {
    return instance.getMessage(key, *params)
  }
}