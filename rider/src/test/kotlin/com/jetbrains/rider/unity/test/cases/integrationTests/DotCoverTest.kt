package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithGeneratedSolutionBase
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.withDcFacade
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("DotCover in Unity")
@Severity(SeverityLevel.CRITICAL)
class DotCoverTest : IntegrationTestWithGeneratedSolutionBase() {
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

    @Test(description = "Check coverage of all tests from Unity solution", enabled = false) // Disabled until merge changes in "Start Unity with Coverage" action
    fun checkCoverAllTestsFromSolution() {
        buildSolutionWithReSharperBuild()
        withDcFacade(project) { ut, dc ->
            ut.waitForDiscovering()
            ut.coverAllTestsInSolution(5)
            dc.waitForTotal(22, goldFile = testGoldFile)
        }
    }
}