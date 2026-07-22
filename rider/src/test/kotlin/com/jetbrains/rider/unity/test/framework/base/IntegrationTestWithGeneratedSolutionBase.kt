package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rdclient.client.frontendProjectSession
import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.facades.build.BuildApiFacade.BuildSettings
import com.jetbrains.rider.test.scriptingApi.RiderUnitTestScriptingFacade
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.waitBackendDocumentChange
import com.jetbrains.rider.test.scriptingApi.waitFirstScriptCompilation
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.unity.test.framework.api.checkSweaInSolution
import com.jetbrains.rider.unity.test.framework.api.killUnity
import com.jetbrains.rider.unity.test.framework.api.refreshUnityModel
import com.jetbrains.rider.unity.test.framework.api.startUnity
import com.jetbrains.rider.unity.test.framework.api.waitConnectionToUnityEditor
import com.jetbrains.rider.unity.test.framework.api.waitForUnityRunConfigurations
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.BeforeEach
import kotlin.io.path.copyTo
import java.nio.file.Path
import kotlin.io.path.absolutePathString
import kotlin.io.path.exists

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

    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        val oldPreprocessTempDirectory = params.preprocessTempDirectory
        params.preprocessTempDirectory = {
            oldPreprocessTempDirectory?.invoke(it)
            val newBehaviourScript = "NewBehaviourScript.cs"
            val sourceScript = testCaseSourceDirectory.resolve(newBehaviourScript)
            if (sourceScript.exists()) {
                sourceScript.copyTo(it.resolve("Assets").resolve(newBehaviourScript), true)
            }
        }
    }

    // Orchestrates the post-open setup steps in order (was a TestNG @BeforeMethod dependsOnMethods chain).
    // The individual steps stay overridable `open fun`s so subclasses can tweak or disable a single step.
    @BeforeEach
    override fun setUpTestCaseSolution() {
        super.setUpTestCaseSolution()
        startUnityProcessAndWait()
        waitForUnityRunConfigurations()
        buildSolutionAfterUnityStarts()
    }

    protected open fun startUnityProcessAndWait() {
        unityProcessHandle = startUnity(withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)

        waitFirstScriptCompilation(project)
        waitConnectionToUnityEditor(project)
    }

    protected open fun waitForUnityRunConfigurations() {
        refreshUnityModel()
        waitForUnityRunConfigurations(project)
    }

    protected open fun buildSolutionAfterUnityStarts() {
        buildSolutionWithReSharperBuild(BuildSettings(ignoreReferencesResolve = true))
    }

    @AfterEach
    fun killUnity() {
        if (::unityProcessHandle.isInitialized) {
            killUnity(unityProcessHandle)
        }
    }

    @AfterEach
    open fun checkSwea() {
        checkSweaInSolution()
    }

    fun waitForDiscoveringWorkaround(file: Path, elementsCount: Int, it: RiderUnitTestScriptingFacade) {
        // see https://youtrack.jetbrains.com/issue/RIDER-55544
        // workaround the situation, when at first assemblies are not compiled, so discovery returns nothing
        // later Unity compiles assemblies, but discovery would not start again, till solution reload
        withOpenedEditor(file.absolutePathString()) {
            FrontendTextControlHost.getInstance(project!!.frontendProjectSession.appSession)
            waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile!!))

            it.waitForDiscovering()
        }
    }
}