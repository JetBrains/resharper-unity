package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.unity.test.framework.api.doFindUsagesTest
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rd.ide.model.findUsagesHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import io.qameta.allure.Description
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Epic(Subsystem.UNITY_FIND_USAGES)
@Feature("Unity AnimImplicitUsage Find Usages")
@Severity(SeverityLevel.NORMAL)
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
    @Description("Test find usages on class")
    fun testOnClass() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "AnimEventHolder")
    }

    @Test()
    @Description("Test find usages on Event")
    fun test01() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEvent")
    }

    @Test(enabled = false) // RIDER-88306 Sorting FindUsages results for non-ProjectFiles
    @Description("Test Sorting FindUsages results")
    fun testSorting() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventDouble")
    }

    @Test()
    @Description("Test find usages on Event with ControllerMod")
    fun testAnimEventWithControllerMod() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventWithControllerMod")
    }

    @Test()
    @Description("Test find usages on Event with ControllerMod and ScriptMod")
    fun testAnimEventWithControllerAndScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithControllerAndScriptMod.cs", "void AnimEventWithControllerAndScriptMod")
    }

    @Test()
    @Description("Test find usages on Event with ScriptMod")
    fun testAnimEventWithScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithScriptMod.cs", "void AnimEventWithScriptMod")
    }
}