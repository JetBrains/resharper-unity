package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.annotations.ChecklistItems
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity Animator Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("AnimatorFindUsages")
open class AnimatorFindUsagesTest : FindUsagesAssetTestBase() {
    @Test(description = "Test animator find usages", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animator Find Usages/on Class"])
    fun animatorFindUsages(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 17, "Behaviour.cs")
    }

    @Test(description = "Test animation find usages for common BehaviorMethod", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animator Find Usages/on Method"])
    fun animationFindUsagesForCommonBehaviorMethod(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 29, "TestScript1.cs")
    }

    @Test(description = "Test animation find usages for common BehaviorFieldValue", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animator Find Usages/on Field"])
    fun animationFindUsagesForCommonBehaviorFieldValue(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 16, "AnimationController.cs")
    }
}