package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import base.integrationTests.preferStandaloneNUnitLauncherInTests
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.withUtFacade
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
    fun checkRunAllTestsFromSolution() = testWithAllTestsInSolution(5)

    @Test(description = "RIDER-46658", enabled = false)
    fun checkTestFixtureAndValueSourceTests() = testWithAllTestsInSolution(14, 16)

    @Test(description = "RIDER-49891", enabled = false)
    fun checkStandaloneNUnitLauncher() {
        preferStandaloneNUnitLauncherInTests()
        testWithAllTestsInSolution(5)
    }

    @Test
    fun checkRunAllTestsFromProject() {
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

    @Test(description = "RIDER-54359")
    fun checkRefreshBeforeTest() {
        withUtFacade(project) {
            it.waitForDiscovering(5)
            val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
            changeFileContent(project, file) {
                it.replace("NewTestScriptSimplePasses", "NewTestScriptSimplePasses2")
            }
            val session = it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }

    private fun testWithAllTestsInSolution(discoveringElements: Int, sessionElements: Int = discoveringElements, successfulTests: Int = sessionElements) {
        withUtFacade(project) {
            it.waitForDiscovering(discoveringElements)
            val session = it.runAllTestsInSolution(
                sessionElements,
                RiderUnitTestScriptingFacade.defaultTimeout,
                successfulTests,
                testGoldFile
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }
}