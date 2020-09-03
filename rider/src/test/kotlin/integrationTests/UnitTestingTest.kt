package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import base.integrationTests.preferStandaloneNUnitLauncherInTests
import com.jetbrains.rider.model.UnitTestLaunchPreference
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
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

    @Test(enabled = false)
    fun checkRunAllTestsFromSolution() = testWithAllTestsInSolution(5)

    @Test(description = "RIDER-46658", enabled = false)
    fun checkTestFixtureAndValueSourceTests() = testWithAllTestsInSolution(14, 16)

    @Test(description = "RIDER-49891", enabled = false)
    fun checkStandaloneNUnitLauncher() {
        preferStandaloneNUnitLauncherInTests()
        testWithAllTestsInSolution(5)
    }

    @Test(enabled = false)
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

    private fun testWithAllTestsInSolution(discoveringElements: Int, sessionElements: Int = discoveringElements, successfulTest: Int = sessionElements) {
        buildSolutionWithReSharperBuild(project)
        withUtFacade(project) {
            it.waitForDiscovering(sessionElements)
            val session = it.runAllTestsInSolution(
                sessionElements,
                RiderUnitTestScriptingFacade.defaultTimeout,
                sessionElements,
                testGoldFile
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }
}