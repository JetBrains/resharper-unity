package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

object TextureVisualizerBundle {
  @NonNls
  private const val BUNDLE = "messages.TextureVisualizerBundle"
  private val instance = DynamicBundle(TextureVisualizerBundle::class.java, BUNDLE)

  @Nls
  fun message(
    @PropertyKey(resourceBundle = BUNDLE) key: String,
    vararg params: Any
  ): String {
    return instance.getMessage(key, *params)
  }
}