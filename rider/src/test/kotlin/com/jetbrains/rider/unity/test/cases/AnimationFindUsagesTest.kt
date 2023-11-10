package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import io.qameta.allure.Description
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test

@Epic(Subsystem.UNITY_FIND_USAGES)
@Feature("Unity Animation Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
open class AnimationFindUsagesTest : FindUsagesAssetTestBase() {
    override fun getSolutionDirectoryName(): String {
        return "AnimationFindUsages"
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animation find usagesfor method")
    fun animationFindUsagesForMethod(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 20, "BehaviourWithMethod.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animation find usages in base class")
    fun animationFindUsagesInBaseClass(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 17, "Base.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animation find usages for property getter")
    fun animationFindUsagesForPropertyGetter(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 14, "BehaviourWithProperty.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animation find usages for property setter")
    fun animationFindUsagesForPropertySetter(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 14, "BehaviourWithProperty.cs")
    }
}