package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithGeneratedSolutionBase
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.withDcFacade
import io.qameta.allure.Description
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test
import java.io.File

@Epic(Subsystem.UNITY_UNIT_TESTING)
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

    @Test(enabled = false) // Disabled until merge changes in "Start Unity with Coverage" action
    @Description("Check coverage of all tests from Unity solution")
    fun checkCoverAllTestsFromSolution() {
        buildSolutionWithReSharperBuild()
        withDcFacade(project) { ut, dc ->
            ut.waitForDiscovering()
            ut.coverAllTestsInSolution(5)
            dc.waitForTotal(22, goldFile = testGoldFile)
        }
    }
}