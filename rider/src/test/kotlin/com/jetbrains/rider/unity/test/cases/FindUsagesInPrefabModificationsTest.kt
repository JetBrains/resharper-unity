package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import org.testng.annotations.Test

@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@TestEnvironment(platform = [PlatformType.ALL])
@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Solution("PrefabModificationTestSolution")
class FindUsagesInPrefabModificationsTest : FindUsagesAssetTestBase() {
    override val traceCategories: List<String>
        get() = super.traceCategories + "JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents"

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification01(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "MethodsContainer.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification02(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "MethodsContainer3.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification03(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(24, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification04(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(29, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification05(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(34, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification06(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(39, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification07(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(45, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification08(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(50, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification09(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(56, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification10(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(61, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification11(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(66, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification12(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(71, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification13(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(76, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification14(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(81, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification15(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script1.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification16(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(10, 29, "Script2.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification17(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script3.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification18(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification19(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "Script5.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification20(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script5.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification21(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(9, 26, "Script5.cs")
    }

// TODO uncomment after fixing tests

//    @Test(dataProvider = "findUsagesGrouping")
//    fun testSingleQuotedName(@Suppress("unused") caseName: String, groups: List<String>?) {
//        disableAllGroups()
//        groups?.forEach { group -> setGroupingEnabled(group, true) }
//
//        doTest(5, 23, "SingleQuotedScript.cs")
//    }
}
