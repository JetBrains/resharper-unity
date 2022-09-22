package base.integrationTests

import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.waitBackendDocumentChange
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File

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

    @BeforeMethod(dependsOnMethods = ["waitForUnityRunConfigurations"])
    fun buildSolutionAfterUnityStarts() {
        buildSolutionWithReSharperBuild(project, ignoreReferencesResolve = true)
    }

    @AfterMethod(alwaysRun = true)
    fun killUnityAndCheckSwea() {
        killUnity(project, unityProcessHandle)
        checkSweaInSolution()
    }

    fun waitForDiscoveringWorkaround(file: File, elementsCount: Int, it: RiderUnitTestScriptingFacade) {
        // see https://youtrack.jetbrains.com/issue/RIDER-55544
        // workaround the situation, when at first assemblies are not compiled, so discovery returns nothing
        // later Unity compiles assemblies, but discovery would not start again, till solution reload
        withOpenedEditor(file.absolutePath) {
            FrontendTextControlHost.getInstance(project!!)
            waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))

            it.waitForDiscovering()
        }
    }
}