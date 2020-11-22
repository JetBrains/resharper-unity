package base.integrationTests

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

    @BeforeMethod
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

    @BeforeMethod(dependsOnMethods = ["startUnityProcessAndWait"])
    fun waitForUnityRunConfigurations() {
        refreshUnityModel()
        waitForUnityRunConfigurations(project)
    }

    @BeforeMethod(dependsOnMethods = ["buildSolutionAfterUnityStarts"])
    fun buildSolutionAfterUnityStarts() {
        buildSolutionWithReSharperBuild(project, ignoreReferencesResolve = true)
    }

    @AfterMethod(alwaysRun = true)
    fun killUnityAndCheckSwea() {
        killUnity(project, unityProcessHandle)
        checkSweaInSolution()
    }
}