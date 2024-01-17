package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.scriptingApi.withUtFacade
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.getUnityDependentGoldFile
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import io.qameta.allure.*
import org.testng.annotations.Test
import java.io.File

@Epic(Subsystem.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution with started Unity2020")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
abstract class UnitTestingTestBase(private val unityVersion: UnityVersion) : IntegrationTestWithUnityProjectBase() {
    override fun getSolutionDirectoryName() = "UnityDebugAndUnitTesting/Project"
    override val unityMajorVersion = this.unityVersion

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DebuggerTestBase::class.simpleName!!)
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

    @Test
    @Description("Check run all tests from project with Unity2020")
    fun checkRunAllTestsFromProject() {
        withUtFacade(project) {
            it.waitForDiscovering()

            val session = it.runAllTestsInProject(
                "Tests",
                5,
                RiderUnitTestScriptingFacade.defaultTimeout,
                5
            )
            it.compareSessionTreeWithGold(session, getUnityDependentGoldFile(unityMajorVersion, unityGoldFile))
        }
    }

    //@Mute("RIDER-95762")
    @Test(description = "RIDER-54359")
    @Description("Check refresh assets before Test")
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
                it.compareSessionTreeWithGold(session2, getUnityDependentGoldFile(unityMajorVersion, unityGoldFile))
            }
        }
    }
}