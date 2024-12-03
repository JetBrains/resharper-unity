package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.annotations.ChecklistItems
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.DOT_NET_6)
@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
class FindUsagesInPrefabModificationsTest : FindUsagesAssetTestBase() {
    override val traceCategories: List<String>
        get() = super.traceCategories + "JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents"

    override val testSolution: String = "PrefabModificationTestSolution"

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification01(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "MethodsContainer.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification02(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "MethodsContainer3.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification03(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(24, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification04(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(29, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification05(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(34, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification06(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(39, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification07(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(45, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification08(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(50, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification09(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(56, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification10(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(61, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification11(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(66, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification12(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(71, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification13(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(76, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification14(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(81, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification15(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script1.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification16(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(10, 29, "Script2.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification17(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script3.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification18(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification19(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "Script5.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification20(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script5.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find Usages In Prefab Modifications"])
    fun findUsagesInPrefabModification21(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(9, 26, "Script5.cs")
    }

// TODO uncomment after fixing tests

//    @Test(dataProvider = "findUsagesGrouping")
//    fun testSingleQuotedName(caseName: String, groups: List<String>?) {
//        disableAllGroups()
//        groups?.forEach { group -> setGroupingEnabled(group, true) }
//
//        doTest(5, 23, "SingleQuotedScript.cs")
//    }
}
