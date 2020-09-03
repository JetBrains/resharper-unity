package base.integrationTests

import com.intellij.execution.RunManager
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.time.Duration

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
        unityProcess = when {
                unityTestEnvironment != null ->
                    startUnity(unityTestEnvironment.resetEditorPrefs, unityTestEnvironment.useRiderTestPath, unityTestEnvironment.batchMode)
                else ->
                    startUnity(resetEditorPrefs, useRiderTestPath, batchMode)
            }

        waitFirstScriptCompilation()
        waitConnection()
    }

    @BeforeMethod(alwaysRun = true, dependsOnMethods = ["startUnityProcessAndWait"])
    fun buildSolution() {
        buildSolutionWithReSharperBuild()
    }

    @BeforeMethod(alwaysRun = true, dependsOnMethods = ["buildSolution"])
    fun waitForUnityRunConfigurations() {
        val runManager = RunManager.getInstance(project)
        waitAndPump(actionsTimeout, { runManager.allConfigurationsList.size >= 2 }) {
            "Unity run configurations didn't appeared, " +
                "current: ${runManager.allConfigurationsList.joinToString(", ", "[", "]")}"
        }
    }
}