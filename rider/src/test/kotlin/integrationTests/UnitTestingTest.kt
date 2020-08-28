package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import org.testng.annotations.Test

class UnitTestingTest : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityUnitTestingProject"

    @Test
    fun checkRunAllTestsFromSolution() {
        buildSolutionWithReSharperBuild()
        withUtFacade(project) {
            it.waitForDiscovering(5)
            val session = it.runAllTestsInSolution(
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5,
                testGoldFile
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }

    @Test
    fun checkRunAllTestsFromProject() {
        buildSolutionWithReSharperBuild()
        withUtFacade(project) {
            it.waitForDiscovering(5)
            val session = it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }

    @Test(description = "RIDER-46658")
    fun checkTestFixtureAndValueSourceTests() {
        replaceFileContent(project, "NewTestScript.cs")
        buildSolutionWithReSharperBuild()
        withUtFacade(project) {
            it.waitForDiscovering(14)
            val session = it.runAllTestsInSolution(
                16,
                RiderUnitTestScriptingFacade.defaultTimeout,
                16,
                testGoldFile
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }
}