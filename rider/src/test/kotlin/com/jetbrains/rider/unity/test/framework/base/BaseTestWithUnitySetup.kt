package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rdclient.testFramework.isUnderTeamCity
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.test.annotations.KnownTestAnnotations
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.enums.TuanjieVersion
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.extensions.SuiteLifecycleExtension
import com.jetbrains.rider.test.logging.TestLoggerHelper
import com.jetbrains.rider.test.scriptingApi.absoluteCanonicalPath
import com.jetbrains.rider.test.scriptingApi.addArgsForUnityProcess
import com.jetbrains.rider.test.scriptingApi.getEngineExecutableInstallationPath
import com.jetbrains.rider.test.shared.utils.ProcessCleanupUtils
import com.jetbrains.rider.test.shared.utils.ProcessCleanupUtils.DefaultPolicy
import org.junit.jupiter.api.extension.BeforeEachCallback
import org.junit.jupiter.api.extension.ExtendWith
import org.junit.jupiter.api.extension.ExtensionContext
import java.lang.reflect.Method
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.TimeUnit
import kotlin.io.path.exists
import kotlin.io.path.readText

@ExtendWith(BaseTestWithUnitySetup.UnitySuiteLifecycleExtension::class, BaseTestWithUnitySetup.UnityLicenseCheckExtension::class)
abstract class BaseTestWithUnitySetup : PerTestSolutionTestBase() {
    /**
     * [BaseTestWithUnitySetup]. Runs once per test run around all classes carrying [BaseTestWithUnitySetup].
     */
    class UnitySuiteLifecycleExtension : SuiteLifecycleExtension {
        override fun beforeSuite(context: ExtensionContext) {
            ProcessCleanupUtils.cleanupSuspiciousProcesses(DefaultPolicy.Unity)
        }

        override fun afterSuite(context: ExtensionContext) {
            ProcessCleanupUtils.cleanupSuspiciousProcesses(DefaultPolicy.Unity)
        }
    }

    /**
     * Checks the license of the Unity Editor the test is going to use. The version is taken from [UnityTestSettings]
     * of the test, and the outcome is remembered for the whole test run, so Unity is launched at most once per version,
     * and a version that has failed the check fails its tests immediately with the stored error.
     */
    class UnityLicenseCheckExtension : BeforeEachCallback {
        override fun beforeEach(context: ExtensionContext) {
            if (!isUnderTeamCity) return
            val settings = KnownTestAnnotations.unityTestSettings(context.requiredTestClass, context.testMethod.orElse(null)).firstOrNull()
                           ?: UnityTestSettings()
            if (settings.tuanjieVersion != TuanjieVersion.NONE) return
            val unityVersion = settings.unityVersion
            val error = licenseCheckResults.computeIfAbsent(unityVersion) { checkUnityEditorLicense(context, unityVersion) }
            if (error.isNotEmpty()) throw IllegalStateException(error)
        }

        /** @return an empty string if the editor is usable, otherwise the error to fail the tests of this version with. */
        private fun checkUnityEditorLicense(context: ExtensionContext, unityVersion: UnityVersion): String {
            val logFile = TestLoggerHelper.getClassLogDirectory(context.requiredTestClass).resolve("UnityEditorCheck-${unityVersion.name}.log")
            val process = try {
                val args = mutableListOf(getEngineExecutableInstallationPath(unityVersion).absoluteCanonicalPath)
                args.addAll(addArgsForUnityProcess(
                    logPath = logFile,
                    resetEditorPrefs = false,
                    useRiderTestPath = false,
                    batchMode = true,
                    generateSolution = false,
                    consistencyCheck = true
                ))
                StartUnityAction.startUnity(args)
            } catch (e: Exception) {
                return "Unity Editor for ${unityVersion.name} is not available: ${e.message}"
            }
            try {
                process?.waitFor(1, TimeUnit.MINUTES)
                if (logFile.exists()) {
                    val logText = logFile.readText()
                    if (logText.contains("No valid Unity Editor license")) {
                        return "Unity License Check has failed for ${unityVersion.name}:\n$logText"
                    }
                }
                return ""
            } finally {
                if (process?.isAlive == true) {
                    process.destroyForcibly()
                }
            }
        }

        private companion object {
            // Per test run; the empty string means a successful check
            val licenseCheckResults = ConcurrentHashMap<UnityVersion, String>()
        }
    }

    protected fun getUnityTestSettingsAnnotation(method: Method? = null): UnityTestSettings =
        KnownTestAnnotations.unityTestSettings(this::class.java, method).firstOrNull() ?: UnityTestSettings()
}