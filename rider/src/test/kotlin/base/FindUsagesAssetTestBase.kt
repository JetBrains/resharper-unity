package base

import base.integrationTests.copyUnityDll
import base.integrationTests.downloadUnityDll
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.DataProvider
import java.io.File
import java.time.Duration

abstract class FindUsagesAssetTestBase : BaseTestWithSolution() {
    protected var unityDll : File? = null

    @DataProvider(name = "findUsagesGrouping")
    fun test1() = arrayOf(
        arrayOf("allGroupsEnabled", arrayOf("SolutionFolder", "Project", "Directory", "File", "Namespace", "Type", "Member", "UnityComponent", "UnityGameObject"))
    )

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)
        if (unityDll == null)
            unityDll = downloadUnityDll()
        copyUnityDll(unityDll!!, activeSolutionDirectory)
    }

    protected fun doTest(line : Int, column : Int, groups: Array<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(line, column)
    }

    protected fun doTest(line : Int, column : Int, fileName : String = "NewBehaviourScript.cs") {
        waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })

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