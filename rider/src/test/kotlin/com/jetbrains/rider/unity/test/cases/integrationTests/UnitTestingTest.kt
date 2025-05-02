package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import com.jetbrains.rider.test.unity.EngineVersion
import com.jetbrains.rider.test.unity.Tuanjie
import com.jetbrains.rider.test.unity.Unity
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution with started Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDebugAndUnitTesting/Project")
abstract class UnitTestingTest(engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {
    @Test(description="Check run all tests from project")
    @ChecklistItems(["Run all tests from the Project"])
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
    @ChecklistItems(["Refresh assets before test"])
    fun checkRefreshBeforeTest() {
        val file = activeSolutionDirectory.resolve("Assets").resolve("Tests").resolve("NewTestScript.cs")
        withOpenedEditor(file.absolutePath) { // the nature of exploration for Unity requires file to be opened
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

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnitTestingTestUnity2020 : UnitTestingTest(Unity.V2020)

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnitTestingTestUnity2022 : UnitTestingTest(Unity.V2022)

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnitTestingTestUnity6 : UnitTestingTest(Unity.V6) {
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Mute("RIDER-113191")
@Solution("TuanjieDebugAndUnitTesting/Project")
class UnitTestingTestTuanjie2022 : UnitTestingTest(Tuanjie.V2022)