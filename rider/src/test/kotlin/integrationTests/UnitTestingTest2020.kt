package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import base.integrationTests.preferStandaloneNUnitLauncherInTests
import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS])
class UnitTestingTest2020 : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "UnitTesting/Project2020"

    @Test
    fun checkRunAllTestsFromProject() {
        withUtFacade(project) {
            val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
            withOpenedEditor(file.absolutePath){
                FrontendTextControlHost.getInstance(project!!)
                waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))

                it.waitForDiscovering(5)
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

    @Test(description = "RIDER-54359")
    fun checkRefreshBeforeTest() {
        withUtFacade(project) {
            // workaround the situation, when at first assenblies are not compiled, so discovery returns nothing
            // later Unity compiles assemblies, but discovery would not start again, till solution reload
            val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
            withOpenedEditor(file.absolutePath){
                FrontendTextControlHost.getInstance(project!!)
                waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))

                it.waitForDiscovering(5)
            }

            it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )

            it.closeAllSessions()

            withOpenedEditor(file.absolutePath){
                changeFileContent(project, file) {
                    it.replace("NewTestScriptSimplePasses(", "NewTestScriptSimplePasses2(")
                }
                FrontendTextControlHost.getInstance(project!!)
                waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))
                it.waitForDiscovering(5)
            }

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