package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.allure.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity Animation Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
open class AnimationFindUsagesTest : FindUsagesAssetTestBase() {
    override fun getSolutionDirectoryName(): String {
        return "AnimationFindUsages"
    }

    @Test(description = "Test animation find usagesfor method", dataProvider = "findUsagesGrouping")
    fun animationFindUsagesForMethod(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 20, "BehaviourWithMethod.cs")
    }

    @Test(description = "Test animation find usages in base class", dataProvider = "findUsagesGrouping")
    fun animationFindUsagesInBaseClass(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 17, "Base.cs")
    }

    @Test(description = "Test animation find usages for property getter", dataProvider = "findUsagesGrouping")
    fun animationFindUsagesForPropertyGetter(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(7, 14, "BehaviourWithProperty.cs")
    }

    @Test(description = "Test animation find usages for property setter", dataProvider = "findUsagesGrouping")
    fun animationFindUsagesForPropertySetter(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 14, "BehaviourWithProperty.cs")
    }
}