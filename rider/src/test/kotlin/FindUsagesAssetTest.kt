import base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.ALL])
open class FindUsagesAssetTest : FindUsagesAssetTestBase() {

    override fun getSolutionDirectoryName(): String {
        return "FindUsages_event_handlers_2017"
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun findScript_2017(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun findEventHandler_2017(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_02_2017")
    fun findScript_02_2017(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_03_2017")
    fun findScript_03_2017(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_04_2017")
    fun findScript_04_2017(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun findScript_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun findEventHandler_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_prefabs_2018")
    fun findEventHandlerPrefabs_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(17, 18)
    }


    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_02_2018")
    fun findScript_02_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_03_2018")
    fun findScript_03_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_04_2018")
    fun findScript_04_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_05_2018")
    fun findScript_05_2018(caseName: String, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }

        doTest(5, 17)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findVoidHandler(caseName: String, groups: Array<String>?) {
        doTest(11, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findIntHandler(caseName: String, groups: Array<String>?) {
        doTest(14, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findFloatHandler(caseName: String, groups: Array<String>?) {
        doTest(17, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findBoolHandler(caseName: String, groups: Array<String>?) {
        doTest(20, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findObjectHandler(caseName: String, groups: Array<String>?) {
        doTest(23, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findUnityEventHandler(caseName: String, groups: Array<String>?) {
        doTest(26, 17, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findPropertyHandler(caseName: String, groups: Array<String>?) {
        doTest(29, 16, groups)
    }

    @Test(dataProvider = "findUsagesGrouping")
    @TestEnvironment(solution = "FindUsages_event_handlers_2018")
    fun findPropertyHandler2(caseName: String, groups: Array<String>?) {
      doTest(33, 16, groups)
    }
}