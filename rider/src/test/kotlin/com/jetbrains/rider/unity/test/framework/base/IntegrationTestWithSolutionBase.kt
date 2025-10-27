package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rdclient.testFramework.isUnderTeamCity
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.logging.TestLoggerHelper
import com.jetbrains.rider.test.scriptingApi.addArgsForUnityProcess
import com.jetbrains.rider.test.scriptingApi.allowUnityPathVfsRootAccess
import com.jetbrains.rider.test.scriptingApi.createLibraryFolderIfNotExist
import com.jetbrains.rider.test.scriptingApi.getEngineExecutableInstallationPath
import com.jetbrains.rider.test.scriptingApi.killHangingUnityProcesses
import com.jetbrains.rider.unity.test.framework.api.activateRiderFrontendTest
import org.testng.annotations.AfterMethod
import org.testng.annotations.AfterSuite
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite
import java.util.concurrent.TimeUnit
import kotlin.collections.filter
import kotlin.collections.forEach
import kotlin.collections.mutableListOf
import kotlin.sequences.filter
import kotlin.text.contains
import kotlin.text.filter

abstract class IntegrationTestWithSolutionBase : PerTestSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        params.waitForCaches = true
        params.preprocessTempDirectory = {
            lifetimeDefinition = LifetimeDefinition()
            allowUnityPathVfsRootAccess(lifetimeDefinition)
            createLibraryFolderIfNotExist(it)
        }
    }

    private lateinit var lifetimeDefinition: LifetimeDefinition

    @BeforeSuite
    fun checkUnityEditorLicense() {
        // Do not check locally and allow missing Unity Installations
        if (!isUnderTeamCity) return
        UnityVersion.entries.filter { it.isUnity() }.forEach { unityVersion ->
            val args = mutableListOf<String>()
            args.add(getEngineExecutableInstallationPath(unityVersion).canonicalPath.toString())
            val logFile = TestLoggerHelper.getClassLogDirectory(javaClass).resolve("UnityEditorCheck.log")
            val unityArgs = addArgsForUnityProcess(logPath = logFile,
                                                   resetEditorPrefs = false, useRiderTestPath = false,
                                                   batchMode = true, generateSolution = false, consistencyCheck = true)
            args.addAll(unityArgs)
            val process = StartUnityAction.startUnity(args)
            try {
                process?.waitFor(1, TimeUnit.MINUTES)
                if (logFile.exists()) {
                    val logText = logFile.readText()
                    // Command line exits with non-zero exit code even if license is valid
                    if (logText.contains("No valid Unity Editor license")) {
                        frameworkLogger.error("Unity License Check has failed for ${unityVersion.name}:\n$logText")
                    }
                }
            }
            finally {
                if (process?.isAlive == true) {
                    process.destroyForcibly()
                }
            }
        }
    }

    @BeforeSuite(alwaysRun = true)
    fun cleanUpUnityProcessesBefore() {
        killHangingUnityProcesses()
    }

    @AfterSuite(alwaysRun = true)
    fun cleanUpUnityProcessesAfter() {
        killHangingUnityProcesses()
    }

    @AfterMethod(alwaysRun = true)
    fun terminateLifetimeDefinition() {
        if(::lifetimeDefinition.isInitialized && lifetimeDefinition.isAlive) {
            lifetimeDefinition.terminate()
        }
    }

    @BeforeMethod
    open fun setUpModelSettings() {
        activateRiderFrontendTest()
    }
}