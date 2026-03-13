package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

class TextureVisualizerBundle : DynamicBundle(BUNDLE) {
    companion object {
        @NonNls
        private const val BUNDLE = "messages.TextureVisualizerBundle"
        private val INSTANCE: TextureVisualizerBundle = TextureVisualizerBundle()

        @Nls
        fun message(
            @PropertyKey(resourceBundle = BUNDLE) key: String,
            vararg params: Any
        ): String {
            return INSTANCE.getMessage(key, *params)
        }
    }
}