package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rd.ide.model.findUsagesHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.junit5.base.PerClassSolutionTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.unity.test.framework.api.doFindUsagesTest
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity AnimImplicitUsage Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("AnimImplicitUsageTest")
@Tag(TeamCityTags.Plugins.Unity)
class AnimImplicitUsageFindUsagesTest : PerClassSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        params.preprocessTempDirectory = { prepareAssemblies(it) }
    }

    override val traceCategories: List<String>
        get() = listOf(
            "JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages")

    @BeforeEach
    fun resetGroupings() {
        project.solution.findUsagesHost.groupingRules.valueOrNull?.items?.forEach { it.enabled.set(true) }
    }

    @Test // Test find usages on class
    @ChecklistItems(["Anim Implicit Usages/on Class"])
    fun testOnClass() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "AnimEventHolder")
    }

    @Test // Test find usages on Event
    @ChecklistItems(["Anim Implicit Usages/on Event"])
    fun test01() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEvent")
    }

    @Test
    @Mute("RIDER-88306 Sorting FindUsages results for non-ProjectFiles") // Test Sorting FindUsages results
    @ChecklistItems(["Anim Implicit Usages/Sorting FindUsages results"])
    fun testSorting() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventDouble")
    }

    @Test // Test find usages on Event with ControllerMod
    @ChecklistItems(["Anim Implicit Usages/on Event with ControllerMod"])
    fun testAnimEventWithControllerMod() {
        doFindUsagesTest("Assets/AnimEventHolder.cs", "void AnimEventWithControllerMod")
    }

    @Test // Test find usages on Event with ControllerMod and ScriptMod
    @ChecklistItems(["Anim Implicit Usages/on Event with ControllerMod and ScriptMod"])
    fun testAnimEventWithControllerAndScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithControllerAndScriptMod.cs", "void AnimEventWithControllerAndScriptMod")
    }

    @Test // Test find usages on Event with ScriptMod
    @ChecklistItems(["Anim Implicit Usages/on Event with ScriptMod"])
    fun testAnimEventWithScriptMod() {
        doFindUsagesTest("Assets/AnimEventHolderWithScriptMod.cs", "void AnimEventWithScriptMod")
    }
}