package com.jetbrains.rider.plugins.unity.ui

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

object UnityUIBundle {
  @NonNls
  private const val BUNDLE = "messages.UnityUIBundle"
  private val instance = DynamicBundle(UnityUIBundle::class.java, BUNDLE)

  @Nls
  fun message(
    @PropertyKey(resourceBundle = BUNDLE) key: String,
    vararg params: Any
  ): String {
    return instance.getMessage(key, *params)
  }
}