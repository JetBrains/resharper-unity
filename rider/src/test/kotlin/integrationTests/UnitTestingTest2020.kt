package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnitTestingTest2020 : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "UnitTesting/Project2020"

    @Test
    fun checkRunAllTestsFromProject() {
        withUtFacade(project) {
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

    @Test(description = "RIDER-54359")
    fun checkRefreshBeforeTest() {
        withUtFacade(project) {
            val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
            waitForDiscoveringWorkaround(file, 5, it)

            it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )

            it.closeAllSessions()

            changeFileContent(project, file) {
                it.replace("NewTestScriptSimplePasses(", "NewTestScriptSimplePasses2(")
            }
            waitForDiscoveringWorkaround(file, 5, it)

            val session2 = it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )

            it.compareSessionTreeWithGold(session2, testGoldFile)
        }
    }
}