package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rd.ide.model.findUsagesHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.base.PerClassSolutionTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.unity.test.framework.api.doFindUsagesTest
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity AnimImplicitUsage Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("AnimImplicitUsageTest")
class AnimImplicitUsageFindUsagesTest : PerClassSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        params.preprocessTempDirectory = { prepareAssemblies(it) }
    }

    override val traceCategories: List<String>
        get() = listOf(
            "JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages")

    @BeforeMethod(alwaysRun = true)
    fun resetGroupings() {
        project.solution.findUsagesHost.groupingRules.valueOrNull?.items?.forEach { it.enabled.set(true) }
    }

    @Test(description="Test find usages on class")
    @ChecklistItems(["Anim Implicit Usages/on Class"])
    fun testOnClass() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "AnimEventHolder")
    }

    @Test(description="Test find usages on Event")
    @ChecklistItems(["Anim Implicit Usages/on Event"])
    fun test01() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEvent")
    }

    @Test(description = "Test Sorting FindUsages results", enabled = false) // RIDER-88306 Sorting FindUsages results for non-ProjectFiles
    @ChecklistItems(["Anim Implicit Usages/Sorting FindUsages results"])
    fun testSorting() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventDouble")
    }

    @Test(description="Test find usages on Event with ControllerMod")
    @ChecklistItems(["Anim Implicit Usages/on Event with ControllerMod"])
    fun testAnimEventWithControllerMod() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventWithControllerMod")
    }

    @Test(description="Test find usages on Event with ControllerMod and ScriptMod")
    @ChecklistItems(["Anim Implicit Usages/on Event with ControllerMod and ScriptMod"])
    fun testAnimEventWithControllerAndScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithControllerAndScriptMod.cs", "void AnimEventWithControllerAndScriptMod")
    }

    @Test(description="Test find usages on Event with ScriptMod")
    @ChecklistItems(["Anim Implicit Usages/on Event with ScriptMod"])
    fun testAnimEventWithScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithScriptMod.cs", "void AnimEventWithScriptMod")
    }
}