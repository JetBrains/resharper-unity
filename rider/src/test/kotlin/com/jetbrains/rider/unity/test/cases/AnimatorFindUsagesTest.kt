package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.DOT_NET_6)
open class AnimatorFindUsagesTest : FindUsagesAssetTestBase() {
    override fun getSolutionDirectoryName(): String {
        return "AnimatorFindUsages"
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun animatorFindUsages(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 17, "Behaviour.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun animationFindUsagesForCommonBehaviorMethod(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 29, "TestScript1.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun animationFindUsagesForCommonBehaviorFieldValue(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(8, 16, "AnimationController.cs")
    }
}