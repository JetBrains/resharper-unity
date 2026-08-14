import com.intellij.DynamicBundle
import org.jetbrains.annotations.Nls
import org.jetbrains.annotations.NonNls
import org.jetbrains.annotations.PropertyKey

object UnityYamlBundle {
  @NonNls
  private const val BUNDLE = "messages.UnityYamlBundle"
  private val instance = DynamicBundle(UnityYamlBundle::class.java, BUNDLE)

  @Nls
  fun message(
    @PropertyKey(resourceBundle = BUNDLE) key: String,
    vararg params: Any
  ): String {
    return instance.getMessage(key, *params)
  }
}