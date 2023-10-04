package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import io.qameta.allure.Description
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test

@Epic(Subsystem.UNITY_FIND_USAGES)
@Feature("Unity Assets Find Usages")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.DOT_NET_6)
open class FindUsagesAssetTest : FindUsagesAssetTestBase() {

    override fun getSolutionDirectoryName(): String {
        return "FindUsages_event_handlers_2017"
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Find script usages with Unity2017 scene model")
    fun findScript_2017(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Find EventHandler usages with Unity2017 scene model")
    fun findEventHandler_2017(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_02_2017")
    @Description("Find script usages with Unity2017 scene model")
    fun findScript_02_2017(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_03_2017")
    @Description("Find script usages with Unity2017 scene model")
    fun findScript_03_2017(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_04_2017")
    @Description("Find script usages with Unity2017 scene model")
    fun findScript_04_2017(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Find script usages with Unity2018 scene model")
    fun findScript_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Find EventHandler usages with Unity2018 scene model")
    fun findEventHandler_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_prefabs_2018")
    @Description("Find EventHandlerPrefabs usages with Unity2018 scene model")
    fun findEventHandlerPrefabs_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }


    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_02_2018")
    @Description("Find script usages with Unity2018 scene model")
    fun findScript_02_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_03_2018")
    @Description("Find script usages with Unity2018 scene model")
    fun findScript_03_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_04_2018")
    @Description("Find script usages with Unity2018 scene model")
    fun findScript_04_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_05_2018")
    @Description("Find script usages with Unity2018 scene model")
    fun findScript_05_2018(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find VoidHandler usages")
    fun findVoidHandler(caseName: String, groups: List<String>?) {
        doTest(11, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find IntHandler usages")
    fun findIntHandler(caseName: String, groups: List<String>?) {
        doTest(14, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find FloatHandler usages")
    fun findFloatHandler(caseName: String, groups: List<String>?) {
        doTest(17, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find BoolHandler usages")
    fun findBoolHandler(caseName: String, groups: List<String>?) {
        doTest(20, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find ObjectHandler usages")
    fun findObjectHandler(caseName: String, groups: List<String>?) {
        doTest(23, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find UnityEventHandler usages")
    fun findUnityEventHandler(caseName: String, groups: List<String>?) {
        doTest(26, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find UnityEventHandler usages")
    fun findPropertyHandler(caseName: String, groups: List<String>?) {
        doTest(29, 16, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    @Description("Find PropertyHandler usages")
    fun findPropertyHandler2(caseName: String, groups: List<String>?) {
        doTest(33, 16, groups)
    }

//    TODO: uncomment when local tests would fine
//    @Test(dataProvider = "findUsagesGrouping")
//    @TestEnvironment(solution = "FindUsagesOverriddenEventHandlers")
//    @Description("Find AssetUsagesForOverriddenEventHandler usages")
//    fun findAssetUsagesForOverriddenEventHandler(caseName: String, groups: List<String>?) {
//        doTest(7, 27, groups, "BaseScript.cs")
//    }
//
//    @Test(dataProvider = "findUsagesGrouping")
//    @TestEnvironment(solution = "FindUsagesOverriddenEventHandlers")
//    @Description("Find AssetUsagesForOverriddenEventHandler usages")
//    fun findAssetUsagesForOverriddenEventHandler2(caseName: String, groups: List<String>?) {
//        doTest(7, 27, groups, "DerivedScript.cs")
//    }
}