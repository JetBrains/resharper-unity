import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rdclient.usages.setRuleEnabled
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.findUsagesHost
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.framework.TeamCityHelper
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.lifetime
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File
import java.nio.file.Path
import java.nio.file.Paths
import kotlin.test.assertNotNull

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
class FindUsagesAssetTest : BaseTestWithSolution() {

    override fun getSolutionDirectoryName(): String {
        return "FindUsages_event_handlers_2017"
    }

    lateinit var unityDll : File

    @BeforeSuite(alwaysRun = true)
    fun getUnityDll() {
        unityDll = DownloadUnityDll()
    }

    @BeforeMethod
    fun InitializeEnvironement() {
        CopyUnityDll(unityDll, project, activeSolutionDirectory)
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