import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.ALL])
class FindUsagesAssetTest : BaseTestWithSolution() {

    override fun getSolutionDirectoryName(): String {
        return "FindUsages_event_handlers_2017"
    }

    lateinit var unityDll : File

    @BeforeSuite(alwaysRun = true)
    fun getUnityDll() {
        unityDll = downloadUnityDll()
    }

    @BeforeMethod
    fun InitializeEnvironement() {
        copyUnityDll(unityDll, project, activeSolutionDirectory)
    }


    @DataProvider(name = "findUsagesGrouping")
    fun test1() = arrayOf(
        arrayOf("allGroupsEnabled", arrayOf("SolutionFolder", "Project", "Directory", "File", "Namespace", "Type", "Member", "UnityComponent", "UnityGameObject"))
    )

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

    private fun doTest(line : Int, column : Int) {
        withOpenedEditor("Assets/NewBehaviourScript.cs") {
            setCaretToPosition(line, column)
            val text = requestFindUsages(activeSolutionDirectory)
            executeWithGold(testGoldFile) { printStream ->
                printStream.print(text)
            }
        }
    }


    private fun disableAllGroups() {
        occurrenceTypeGrouping(false)
        solutionFolderGrouping(false)
        projectGrouping(false)
        directoryGrouping(false)
        fileGrouping(false)
        namespaceGrouping(false)
        typeGrouping(false)
        unityGameObjectGrouping(false)
        unityComponentGrouping(false)
    }

    fun BaseTestWithSolution.unityGameObjectGrouping(enable: Boolean) = setGroupingEnabled("UnityGameObject", enable)

    fun BaseTestWithSolution.unityComponentGrouping(enable: Boolean) = setGroupingEnabled("UnityComponent", enable)

    override val waitForCaches = true
}