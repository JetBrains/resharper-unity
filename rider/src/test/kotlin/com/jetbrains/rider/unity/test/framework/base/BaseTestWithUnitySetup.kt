package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rdclient.testFramework.isUnderTeamCity
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.test.annotations.KnownTestAnnotations
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.junit5.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.junit5.extensions.SuiteLifecycleExtension
import com.jetbrains.rider.test.logging.TestLoggerHelper
import com.jetbrains.rider.test.scriptingApi.absoluteCanonicalPath
import com.jetbrains.rider.test.scriptingApi.addArgsForUnityProcess
import com.jetbrains.rider.test.scriptingApi.getEngineExecutableInstallationPath
import com.jetbrains.rider.test.shared.utils.ProcessCleanupUtils
import com.jetbrains.rider.test.shared.utils.ProcessCleanupUtils.DefaultPolicy
import org.junit.jupiter.api.extension.ExtendWith
import org.junit.jupiter.api.extension.ExtensionContext
import java.lang.reflect.Method
import java.util.concurrent.TimeUnit
import kotlin.io.path.exists
import kotlin.io.path.readText

@ExtendWith(BaseTestWithUnitySetup.UnitySuiteLifecycleExtension::class)
abstract class BaseTestWithUnitySetup : PerTestSolutionTestBase() {
    /**
     * [BaseTestWithUnitySetup]. Runs once per test run around all classes carrying [BaseTestWithUnitySetup].
     */
    class UnitySuiteLifecycleExtension : SuiteLifecycleExtension {
        override fun beforeSuite(context: ExtensionContext) {
            checkUnityEditorLicense(context)
            ProcessCleanupUtils.cleanupSuspiciousProcesses(DefaultPolicy.Unity)
        }

        override fun afterSuite(context: ExtensionContext) {
            ProcessCleanupUtils.cleanupSuspiciousProcesses(DefaultPolicy.Unity)
        }

        private fun checkUnityEditorLicense(context: ExtensionContext) {
            if (!isUnderTeamCity) return
            UnityVersion.entries.filter { it.isUnity() }.forEach { unityVersion ->
                val args = mutableListOf<String>()
                args.add(getEngineExecutableInstallationPath(unityVersion).absoluteCanonicalPath)
                val logFile = TestLoggerHelper.getClassLogDirectory(context.requiredTestClass).resolve("UnityEditorCheck.log")
                val unityArgs = addArgsForUnityProcess(
                    logPath = logFile,
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
    }

    protected fun getUnityTestSettingsAnnotation(method: Method? = null): UnityTestSettings =
        KnownTestAnnotations.unityTestSettings(this::class.java, method).firstOrNull() ?: UnityTestSettings()
}