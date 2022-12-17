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

    @Test
    fun test03() {
        disableAllGroups()
        doTest(6, 20, "EventTrigger/even.cs")
    }

    @Test
    fun test031() {
        disableAllGroups()
        doTest(11, 20, "EventTrigger/even.cs")
    }

    @Test
    fun test032() {
        disableAllGroups()
        doTest(16, 20, "EventTrigger/even.cs")
    }

    @Test
    fun test033() {
        disableAllGroups()
        doTest(21, 30, "EventTrigger/even.cs")
    }

    @Test
    fun test04() {
        disableAllGroups()
        doTest(6, 20, "EventTrigger/NewBehaviourScript.cs")
        //doTest(11, 20, "EventTrigger/NewBehaviourScript.cs")
    }

    @Test
    fun test041() {
        disableAllGroups()
        doTest(11, 25, "EventTrigger/NewBehaviourScript.cs")
    }
}
