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
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity Animation Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("AnimationFindUsages")
open class AnimationFindUsagesTest : FindUsagesAssetTestBase() {
    @Test(description = "Test animation find usages for method", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Method"])
    fun animationFindUsagesForMethod(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 20, "BehaviourWithMethod.cs")
    }

    @Test(description = "Test animation find usages in base class", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Base Class"])
    fun animationFindUsagesInBaseClass(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 17, "Base.cs")
    }

    @Test(description = "Test animation find usages for property getter", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Property Getter"])
    fun animationFindUsagesForPropertyGetter(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 14, "BehaviourWithProperty.cs")
    }

    @Test(description = "Test animation find usages for property setter", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Property Setter"])
    fun animationFindUsagesForPropertySetter(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 14, "BehaviourWithProperty.cs")
    }
}