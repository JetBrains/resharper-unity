package integrationTests

import base.integrationTests.IntegrationTestWithEditorBase
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

    @Test
    fun checkCoverAllTestsFromSolution() {
        withDcFacade(project) { ut, dc ->
            ut.waitForDiscovering(5)
            ut.coverAllTestsInSolution(5)
            dc.compareCoverageTreeWithGold(testGoldFile)
        }
    }
}