package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution with started Unity2020")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
abstract class UnitTestingTestBase(private val engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase() {
    override fun getSolutionDirectoryName(): String {
        return if (engineVersion.isTuanjie()) {
            "TuanjieDebugAndUnitTesting/Project"
        }
        else {
            "UnityDebugAndUnitTesting/Project"
        }
    }
    override val majorVersion = this.engineVersion

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(UnitTestingTestBase::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testStorage.testMethod.name).combine("source")

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newBehaviourScript = "NewBehaviourScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newBehaviourScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve(newBehaviourScript), true)
        }
    }

    @Test(description="Check run all tests from project with Unity2020")
    fun checkRunAllTestsFromProject() {
        withUtFacade(project) {
            it.waitForDiscovering()

            val session = it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )
            it.compareSessionTreeWithGold(session, testGoldFile)
        }
    }

    //@Mute("RIDER-95762")
    @Test(description = "RIDER-54359. Check refresh assets before Test")
    fun checkRefreshBeforeTest() {
        val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
        withOpenedEditor(project, file.absolutePath) { // the nature of exploration for Unity requires file to be opened
            withUtFacade(project!!) {
                it.waitForDiscovering()
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

                this.waitForDaemon()

                it.activateExplorer()
                it.waitForDiscovering()

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
}