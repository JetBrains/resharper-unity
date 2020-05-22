import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeSuite
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

open abstract class FindUsagesAssetTestBase : BaseTestWithSolution() {
    lateinit var unityDll : File

    @DataProvider(name = "findUsagesGrouping")
    fun test1() = arrayOf(
        arrayOf("allGroupsEnabled", arrayOf("SolutionFolder", "Project", "Directory", "File", "Namespace", "Type", "Member", "UnityComponent", "UnityGameObject"))
    )

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)
        copyUnityDll(unityDll, activeSolutionDirectory)
    }

    protected fun doTest(line : Int, column : Int, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(line, column)
    }

    protected fun doTest(line : Int, column : Int, fileName : String = "NewBehaviourScript.cs") {
        waitAndPump(project.lifetime, { project.solution.rdUnityModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })

        withOpenedEditor("Assets/$fileName") {
            setCaretToPosition(line, column)
            val text = requestFindUsages(activeSolutionDirectory)
            executeWithGold(testGoldFile) { printStream ->
                printStream.print(text)
            }
        }
    }

    protected fun disableAllGroups() {
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

    private fun BaseTestWithSolution.unityGameObjectGrouping(enable: Boolean) = setGroupingEnabled("UnityGameObject", enable)
    private fun BaseTestWithSolution.unityComponentGrouping(enable: Boolean) = setGroupingEnabled("UnityComponent", enable)

    override val waitForCaches = true
}