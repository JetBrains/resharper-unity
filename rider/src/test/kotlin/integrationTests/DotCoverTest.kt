package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.withDcFacade
import org.testng.annotations.Test
import java.io.File

class DotCoverTest : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityUnitTestingProject"

    override val withCoverage: Boolean
        get() = true

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newTestScript = "NewTestScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newTestScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve("Tests").resolve(newTestScript), true)
        }
    }

    @Test(enabled = false) // Disabled until merge changes in "Start Unity with Coverage" action
    fun checkCoverAllTestsFromSolution() {
        buildSolutionWithReSharperBuild()
        withDcFacade(project) { ut, dc ->
            ut.waitForDiscovering()
            ut.coverAllTestsInSolution(5)
            dc.waitForTotal(22, goldFile = testGoldFile)
        }
    }
}