
import base.doFindUsagesTest
import base.integrationTests.prepareAssemblies
import com.jetbrains.rd.ide.model.findUsagesHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class AnimImplicitUsageFindUsagesTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "AnimImplicitUsageTest"
    }

    override fun preprocessTempDirectory(tempDir: File) {
        prepareAssemblies(activeSolutionDirectory)
    }

    override val traceCategories: List<String>
        get() = listOf(
            "JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages")

    @BeforeMethod(alwaysRun = true)
    fun resetGroupings() {
        project.solution.findUsagesHost.groupingRules.valueOrNull?.items?.forEach { it.enabled.set(true) }
    }

    @Test()
    fun testOnClass() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "AnimEventHolder")
    }

    @Test()
    fun test01() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEvent")
    }

    @Test(enabled = false) // RIDER-88306 Sorting FindUsages results for non-ProjectFiles
    fun testSorting() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventDouble")
    }

    @Test()
    fun testAnimEventWithControllerMod() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventWithControllerMod")
    }

    @Test()
    fun testAnimEventWithControllerAndScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithControllerAndScriptMod.cs", "void AnimEventWithControllerAndScriptMod")
    }

    @Test()
    fun testAnimEventWithScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithScriptMod.cs", "void AnimEventWithScriptMod")
    }
}