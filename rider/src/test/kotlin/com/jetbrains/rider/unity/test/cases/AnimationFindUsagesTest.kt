package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import org.junit.jupiter.api.Tag
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.MethodSource

@Tag(TeamCityTags.Plugins.Unity)
@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity Animation Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("AnimationFindUsages")
open class AnimationFindUsagesTest : FindUsagesAssetTestBase() {
    @ParameterizedTest(name = "{0}") // Test animation find usages for method
    @MethodSource("findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Method"])
    fun animationFindUsagesForMethod(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 20, "BehaviourWithMethod.cs")
    }

    @ParameterizedTest(name = "{0}") // Test animation find usages in base class
    @MethodSource("findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Base Class"])
    fun animationFindUsagesInBaseClass(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 17, "Base.cs")
    }

    @ParameterizedTest(name = "{0}") // Test animation find usages for property getter
    @MethodSource("findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Property Getter"])
    fun animationFindUsagesForPropertyGetter(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 14, "BehaviourWithProperty.cs")
    }

    @ParameterizedTest(name = "{0}") // Test animation find usages for property setter
    @MethodSource("findUsagesGrouping")
    @ChecklistItems(["Animation Find Usages/on Property Setter"])
    fun animationFindUsagesForPropertySetter(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 14, "BehaviourWithProperty.cs")
    }
}
