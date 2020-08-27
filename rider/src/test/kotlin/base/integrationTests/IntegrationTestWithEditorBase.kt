package base.integrationTests

import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod

abstract class IntegrationTestWithEditorBase : IntegrationTestBase() {
    protected open val resetEditorPrefs: Boolean
        get() = false

    protected open val useRiderTestPath: Boolean
        get() = false

    protected open val batchMode: Boolean
        get() = true

    private lateinit var unityProcess: Process

    @AfterMethod(alwaysRun = true)
    fun killUnityAndCheckSwea() {
        killUnity(unityProcess)
        checkSweaInSolution()
    }

    @BeforeMethod(alwaysRun = true)
    fun startUnityProcessAndWait() {
        installPlugin()
        val unityTestEnvironment = testMethod.unityEnvironment
        unityProcess =
            if (unityTestEnvironment != null)
                startUnity(unityTestEnvironment.resetEditorPrefs, unityTestEnvironment.useRiderTestPath, unityTestEnvironment.batchMode)
            else
                startUnity(resetEditorPrefs, useRiderTestPath, batchMode)

        waitFirstScriptCompilation()
        waitConnection()
    }

    @BeforeMethod(alwaysRun = true, dependsOnMethods = ["startUnityProcessAndWait"])
    fun buildSolution() {
        buildSolutionWithReSharperBuild()
    }
}