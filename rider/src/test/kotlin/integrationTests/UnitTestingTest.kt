package integrationTests

import base.integrationTests.IntegrationTestWithGeneratedSolutionBase
import base.integrationTests.preferStandaloneNUnitLauncherInTests
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnitTestingTest : IntegrationTestWithGeneratedSolutionBase() {
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
            // workaround the situation, when at first assenblies are not compiled, so discovery returns nothing
            // later Unity compiles assemblies, but discovery would not start again, till solution reload
            val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
            waitForDiscoveringWorkaround(file, 5, it)

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
            val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
            waitForDiscoveringWorkaround(file, 5, it)

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