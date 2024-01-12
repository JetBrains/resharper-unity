package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.waitBackendDocumentChange
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.unityEnvironment
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File

/**
 * Class is used in tests where initial sln/csproj structure exists. We might regenerate afterwards, but Rider is opened
 * first. This was done initially to be able to use whole available API from the Rider, which is not available before project is opened.
 */
abstract class IntegrationTestWithGeneratedSolutionBase : IntegrationTestWithSolutionBase() {
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
    open fun startUnityProcessAndWait() {
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
    open fun waitForUnityRunConfigurations() {
        refreshUnityModel()
        waitForUnityRunConfigurations(project)
    }

    @BeforeMethod(dependsOnMethods = ["waitForUnityRunConfigurations"])
    open fun buildSolutionAfterUnityStarts() {
        buildSolutionWithReSharperBuild(project, ignoreReferencesResolve = true)
    }

    @AfterMethod(alwaysRun = true)
    fun killUnity() {
        if (::unityProcessHandle.isInitialized) {
            killUnity(unityProcessHandle)
        }
    }

    @AfterMethod
    open fun checkSwea() {
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