package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.unity.test.framework.base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_FIND_USAGES)
@Feature("Unity Find Usages with prefab modifications")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.DOT_NET_6)
class FindUguiUsagesPrefabModificationsTest : FindUsagesAssetTestBase() {

    override val traceCategories: List<String>
        get() = super.traceCategories + "JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents"

    override val testSolution: String = "UI_PrefabModifications" 

    @Test(description="Find Usages for event OnClickFromPrefabModification")
    fun test01() {
        disableAllGroups()
        doTest(5, 20, "PlayerInput.cs")
    }

    @Test(description="Find Usages for event OnClickFromPrefab")
    fun test02() {
        disableAllGroups()
        doTest(10, 20, "PlayerInput.cs")
    }

    @Test(description="Find Usages for event")
    fun test03() {
        disableAllGroups()
        doTest(6, 20, "EventTrigger/even.cs")
    }

    @Test(description="Find Usages for event")
    fun test031() {
        disableAllGroups()
        doTest(11, 20, "EventTrigger/even.cs")
    }

    @Test(description="Find Usages for event")
    fun test032() {
        disableAllGroups()
        doTest(16, 20, "EventTrigger/even.cs")
    }

    @Test(description="Find Usages for event")
    fun test033() {
        disableAllGroups()
        doTest(21, 30, "EventTrigger/even.cs")
    }

    @Test(description="Find Usages for event")
    fun test04() {
        disableAllGroups()
        doTest(6, 20, "EventTrigger/NewBehaviourScript.cs")
        //doTest(11, 20, "EventTrigger/NewBehaviourScript.cs")
    }

    @Test(description="Find Usages for event")
    fun test041() {
        disableAllGroups()
        doTest(11, 25, "EventTrigger/NewBehaviourScript.cs")
    }
}
