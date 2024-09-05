package com.jetbrains.rider.unity.test.framework.base

import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.DataProvider
import java.io.File
import java.time.Duration

abstract class FindUsagesAssetTestBase : BaseTestWithSolution() {
    @DataProvider(name = "findUsagesGrouping")
    fun test1() = arrayOf(
        arrayOf("allGroupsEnabled", listOf("SolutionFolder", "Project", "Directory", "File", "Namespace", "Type", "Member", "UnityComponent", "UnityGameObject"))
    )

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)
        prepareAssemblies(tempDir)
    }

    protected fun doTest(line : Int, column : Int, groups: List<String>?, fileName : String = "NewBehaviourScript.cs") {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(line, column, fileName)
    }

    protected fun doTest(line : Int, column : Int, fileName : String = "NewBehaviourScript.cs") {
        waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })

        withOpenedEditor("Assets/$fileName") {
            setCaretToPosition(line, column)
            val text = requestFindUsages(activeSolutionDirectory, true)
            executeWithGold(testGoldFile) { printStream ->
                printStream.print(text)
            }
        }
    }

    protected fun disableAllGroups() {
        disableAllFindUsagesGroups(project)
        unityGameObjectGrouping(false)
        unityComponentGrouping(false)
    }

    private fun BaseTestWithSolution.unityGameObjectGrouping(enable: Boolean) = setGroupingEnabled("UnityGameObject", enable)
    private fun BaseTestWithSolution.unityComponentGrouping(enable: Boolean) = setGroupingEnabled("UnityComponent", enable)

    override val waitForCaches = true
}