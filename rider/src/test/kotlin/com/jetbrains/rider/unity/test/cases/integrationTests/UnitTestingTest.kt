package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.TuanjieVersion
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution with started Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDebugAndUnitTesting/Project")
abstract class UnitTestingTest() : IntegrationTestWithUnityProjectBase() {
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
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class UnitTestingTestUnity2022 : UnitTestingTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6)
class UnitTestingTestUnity6 : UnitTestingTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_2)
class UnitTestingTestUnity6_2 : UnitTestingTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_3)
class UnitTestingTestUnity6_3 : UnitTestingTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Mute("RIDER-113191")
@Solution("TuanjieDebugAndUnitTesting/Project")
@UnityTestSettings(tuanjieVersion = TuanjieVersion.V2022)
class UnitTestingTestTuanjie2022 : UnitTestingTest()