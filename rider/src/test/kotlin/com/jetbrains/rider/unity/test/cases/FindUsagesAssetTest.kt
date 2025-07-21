package com.jetbrains.rider.unity.test.cases

import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity Assets Find Usages")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@TestRequirements(platform = [PlatformType.ALL])
@Solution("FindUsages_event_handlers_2017")
open class FindUsagesAssetTest : FindUsagesAssetTestBase() {
    @Test(description = "Find script usages with Unity2017 scene model", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find usages for Assets/Unity2017/Script usages"])
    fun findScript_2017(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find EventHandler usages with Unity2017 scene model", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find usages for Assets/Unity2017/EventHandler usages"])
    fun findEventHandler_2017(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }

    @Test(description = "Find script usages with Unity2017 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_02_2017")
    @ChecklistItems(["Find usages for Assets/Unity2017/Script usages"])
    fun findScript_02_2017(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find script usages with Unity2017 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_03_2017")
    @ChecklistItems(["Find usages for Assets/Unity2017/Script usages"])
    fun findScript_03_2017(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find script usages with Unity2017 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_04_2017")
    @ChecklistItems(["Find usages for Assets/Unity2017/Script usages"])
    fun findScript_04_2017(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find script usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find usages for Assets/Unity2018/Script usages"])
    fun findScript_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find EventHandler usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @ChecklistItems(["Find usages for Assets/Unity2018/EventHandler usages"])
    fun findEventHandler_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }

    @Test(description = "Find EventHandlerPrefabs usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_prefabs_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/EventHandlerPrefabs usages"])
    fun findEventHandlerPrefabs_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }


    @Test(description = "Find script usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_02_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/Script usages"])
    fun findScript_02_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find script usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_03_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/Script usages"])
    fun findScript_03_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find script usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_04_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/Script usages"])
    fun findScript_04_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find script usages with Unity2018 scene model", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_05_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/Script usages"])
    fun findScript_05_2018(@Suppress("unused") caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(description = "Find VoidHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/VoidHandler usages"])
    fun findVoidHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(11, 17, groups)
    }

    @Test(description = "Find IntHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/IntHandler usages"])
    fun findIntHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(14, 17, groups)
    }

    @Test(description = "Find FloatHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/FloatHandler usages"])
    fun findFloatHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(17, 17, groups)
    }

    @Test(description = "Find BoolHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/BoolHandler usages"])
    fun findBoolHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(20, 17, groups)
    }

    @Test(description = "Find ObjectHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/ObjectHandler usages"])
    fun findObjectHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(23, 17, groups)
    }

    @Test(description = "Find UnityEventHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/UnityEventHandler usages"])
    fun findUnityEventHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(26, 17, groups)
    }

    @Test(description = "Find UnityEventHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/UnityEventHandler usages"])
    fun findPropertyHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(29, 16, groups)
    }

    @Test(description = "Find PropertyHandler usages", dataProvider = "findUsagesGrouping")
    @Solution("FindUsages_event_handlers_2018")
    @ChecklistItems(["Find usages for Assets/Unity2018/PropertyHandler usages"])
    fun findPropertyHandler2(@Suppress("unused") caseName: String, groups: List<String>?) {
        doTest(33, 16, groups)
    }

//    TODO: uncomment when local tests would fine
//    @Test(description = "Find AssetUsagesForOverriddenEventHandler usages", dataProvider = "findUsagesGrouping")
//    @Solution("FindUsagesOverriddenEventHandlers")
//    fun findAssetUsagesForOverriddenEventHandler(@Suppress("unused") caseName: String, groups: List<String>?) {
//        doTest(7, 27, groups, "BaseScript.cs")
//    }
//
//    @Test(description = "Find AssetUsagesForOverriddenEventHandler usages", dataProvider = "findUsagesGrouping")
//    @Solution("FindUsagesOverriddenEventHandlers")
//    fun findAssetUsagesForOverriddenEventHandler2(@Suppress("unused") caseName: String, groups: List<String>?) {
//        doTest(7, 27, groups, "DerivedScript.cs")
//    }
}