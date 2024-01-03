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
@Feature("Unity Animator Find Usages")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.DOT_NET_6)
open class AnimatorFindUsagesTest : FindUsagesAssetTestBase() {
    override fun getSolutionDirectoryName(): String {
        return "AnimatorFindUsages"
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animator find usages")
    fun animatorFindUsages(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 17, "Behaviour.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animation find usages for common BehaviorMethod")
    fun animationFindUsagesForCommonBehaviorMethod(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 29, "TestScript1.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    @Description("Test animation find usages for common BehaviorFieldValue")
    fun animationFindUsagesForCommonBehaviorFieldValue(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 16, "AnimationController.cs")
    }
}