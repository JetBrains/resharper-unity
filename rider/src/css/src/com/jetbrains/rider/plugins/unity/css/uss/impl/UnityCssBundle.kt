package com.jetbrains.rider.plugins.unity.css.uss.impl

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

object UnityCssBundle {
  @NonNls
  private const val BUNDLE = "messages.UnityCssBundle"
  private val instance = DynamicBundle(UnityCssBundle::class.java, BUNDLE)

  @Nls
  fun message(
    @PropertyKey(resourceBundle = BUNDLE) key: String,
    vararg params: Any
  ): String {
    return instance.getMessage(key, *params)
  }
}