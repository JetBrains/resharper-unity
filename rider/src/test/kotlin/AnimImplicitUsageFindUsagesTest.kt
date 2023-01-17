import base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6)
open class AnimImplicitUsageFindUsagesTest : FindUsagesAssetTestBase() {
    override fun getSolutionDirectoryName(): String {
        return "AnimImplicitUsageTest"
    }

    @Test()
    fun test01() {
        disableAllGroups()
        doTest(10, 15, "AnimEventHolder.cs")
    }
}