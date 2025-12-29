package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rdclient.testFramework.isUnderTeamCity
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.test.annotations.KnownTestAnnotations
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.logging.TestLoggerHelper
import com.jetbrains.rider.test.scriptingApi.addArgsForUnityProcess
import com.jetbrains.rider.test.scriptingApi.getEngineExecutableInstallationPath
import com.jetbrains.rider.test.shared.utils.ProcessCleanupUtils
import com.jetbrains.rider.test.shared.utils.ProcessCleanupUtils.DefaultPolicy
import org.testng.annotations.AfterSuite
import org.testng.annotations.BeforeSuite
import java.lang.reflect.Method
import java.util.concurrent.TimeUnit
import kotlin.collections.filter
import kotlin.collections.forEach
import kotlin.collections.mutableListOf
import kotlin.io.path.exists
import kotlin.io.path.readText
import kotlin.text.contains

open class BaseTestWithUnitySetup : PerTestSolutionTestBase() {

    @BeforeSuite
    fun checkUnityEditorLicense() {
        if (!isUnderTeamCity) return
        UnityVersion.entries.filter { it.isUnity() }.forEach { unityVersion ->
            val args = mutableListOf<String>()
            args.add(getEngineExecutableInstallationPath(unityVersion).canonicalPath.toString())
            val logFile = TestLoggerHelper.getClassLogDirectory(javaClass).resolve("UnityEditorCheck.log")
            val unityArgs = addArgsForUnityProcess(
                logPath = logFile.toFile(),
                resetEditorPrefs = false,
                useRiderTestPath = false,
                batchMode = true,
                generateSolution = false,
                consistencyCheck = true
            )
            args.addAll(unityArgs)
            val process = StartUnityAction.startUnity(args)
            try {
                process?.waitFor(1, TimeUnit.MINUTES)
                if (logFile.exists()) {
                    val logText = logFile.readText()
                    if (logText.contains("No valid Unity Editor license")) {
                        frameworkLogger.error("Unity License Check has failed for ${unityVersion.name}:\n$logText")
                    }
                }
            } finally {
                if (process?.isAlive == true) {
                    process.destroyForcibly()
                }
            }
        }
    }

    @BeforeSuite(alwaysRun = true)
    fun cleanUpUnityProcessesBefore() {
        ProcessCleanupUtils.cleanupSuspiciousProcesses(DefaultPolicy.Unity)
    }

    @AfterSuite(alwaysRun = true)
    fun cleanUpUnityProcessesAfter() {
        ProcessCleanupUtils.cleanupSuspiciousProcesses(DefaultPolicy.Unity)
    }

    protected fun getUnityTestSettingsAnnotation(method: Method? = null): UnityTestSettings =
        KnownTestAnnotations.unityTestSettings(this::class.java, method).firstOrNull() ?: UnityTestSettings()
}