package base.integrationTests

import com.jetbrains.rider.test.scriptingApi.buildSolutionWithConsoleBuild
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod

abstract class IntegrationTestWithEditorBase : IntegrationTestWithSolutionBase() {
    protected open val withCoverage: Boolean
        get() = false

    protected open val resetEditorPrefs: Boolean
        get() = false

    protected open val useRiderTestPath: Boolean
        get() = false

    protected open val batchMode: Boolean
        get() = true

    private lateinit var unityProcessHandle: ProcessHandle

    @AfterMethod(alwaysRun = true)
    fun killUnityAndCheckSwea() {
        killUnity(unityProcessHandle)
        checkSweaInSolution()
    }

    @BeforeMethod(alwaysRun = true)
    fun startUnityProcessAndWait() {
        installPlugin()
        val unityTestEnvironment = testMethod.unityEnvironment
        unityProcessHandle = when {
            unityTestEnvironment != null ->
                startUnity(
                    unityTestEnvironment.withCoverage,
                    unityTestEnvironment.resetEditorPrefs,
                    unityTestEnvironment.useRiderTestPath,
                    unityTestEnvironment.batchMode
                )
            else ->
                startUnity(withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)
        }

        waitFirstScriptCompilation(project)
        waitConnectionToUnityEditor(project)
    }

    @BeforeMethod(alwaysRun = true, dependsOnMethods = ["startUnityProcessAndWait"])
    fun buildSolutionAfterUnityStarts() {
        buildSolutionWithConsoleBuild()
    }

    @BeforeMethod(alwaysRun = true, dependsOnMethods = ["buildSolutionAfterUnityStarts"])
    fun waitForUnityRunConfigurations() {
        waitForUnityRunConfigurations(project)
    }
}