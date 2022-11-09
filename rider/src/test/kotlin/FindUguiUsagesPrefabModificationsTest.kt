import base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.ALL], toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6)
class FindUguiUsagesPrefabModificationsTest : FindUsagesAssetTestBase() {

    override val traceCategories: List<String>
        get() = super.traceCategories + "JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents"

    override fun getSolutionDirectoryName(): String {
        return "UI_PrefabModifications"
    }

    @Test
    fun test01() {
        disableAllGroups()
        doTest(5, 20, "PlayerInput.cs")
    }

    @Test
    fun test02() {
        disableAllGroups()
        doTest(10, 20, "PlayerInput.cs")
    }
}
