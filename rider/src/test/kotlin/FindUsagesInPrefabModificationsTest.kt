import base.FindUsagesAssetTestBase
import base.downloadUnityDll
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.BeforeSuite
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.ALL])
class FindUsagesInPrefabModificationsTest : FindUsagesAssetTestBase() {

    @BeforeSuite(alwaysRun = true)
    fun getUnityDll() {
        unityDll = downloadUnityDll()
    }

    override fun getSolutionDirectoryName(): String {
        return "PrefabModificationTestSolution"
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test01(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "MethodsContainer.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test02(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "MethodsContainer3.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test03(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(24, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test04(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(29, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test05(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(34, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test06(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(39, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test07(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(45, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test08(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(50, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test09(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(56, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test10(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(61, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test11(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(66, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test12(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(71, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test13(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(76, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test14(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(81, 25, "MethodsContainer4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test15(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script1.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test16(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(10, 29, "Script2.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test17(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script3.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test18(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script4.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test19(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(7, 29, "Script5.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test20(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(8, 29, "Script5.cs")
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun test21(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(9, 26, "Script5.cs")
    }
}