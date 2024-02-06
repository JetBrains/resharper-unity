package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import com.jetbrains.rider.unity.test.framework.api.preferStandaloneNUnitLauncherInTests
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithGeneratedSolutionBase
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution without started Unity")
@Severity(SeverityLevel.CRITICAL)
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

    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
          description = "Check run all tests from solution")
    fun checkRunAllTestsFromSolution() = testWithAllTestsInSolution(5)

    @Test(description = "Check test fixture and value source. RIDER-46658", enabled = false)
    fun checkTestFixtureAndValueSourceTests() = testWithAllTestsInSolution(14, 16)

    @Test(description = "Check Standalone NUnit launcher. RIDER-49891", enabled = false)
    fun checkStandaloneNUnitLauncher() {
        preferStandaloneNUnitLauncherInTests()
        testWithAllTestsInSolution(5)
    }

    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
          description = "Check run all tests from project")
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

    private fun testWithAllTestsInSolution(discoveringElements: Int,
                                           sessionElements: Int = discoveringElements,
                                           successfulTests: Int = sessionElements) {
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