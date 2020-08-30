package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS])
class UnitTestingTest : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityUnitTestingProject"

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newTestScript = "NewTestScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newTestScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve("Tests").resolve(newTestScript), true)
        }
    }

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
        rebuildSolutionWithReSharperBuild()

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