package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.notification.Notification
import com.intellij.notification.Notifications
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.testFramework.common.ThreadLeakTracker
import com.intellij.util.system.OS
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityScriptingBackend
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.util.UnityPlayerRuntimeDetector
import com.jetbrains.rider.test.annotations.RiderTestTimeout
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.UnityBackend
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.framework.flushQueues
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.debugProgram
import com.jetbrains.rider.test.scriptingApi.selectRunConfiguration
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.utils.NullPrintStream
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withContext
import org.junit.jupiter.api.Nested
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path
import java.time.Duration
import java.util.concurrent.CopyOnWriteArrayList
import java.util.concurrent.TimeUnit
import kotlin.io.path.absolutePathString
import kotlin.io.path.name
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

/**
 * Checks that Rider reads the scripting backend of a Unity player, and that it warns the user when
 * the backend is IL2CPP.
 *
 * An IL2CPP player supports attach only, so `UnityExeDebugProfileState.execute` raises a warning when
 * the user starts such a player under the debugger. [UnityPlayerRuntimeDetector] supplies the backend
 * that the warning depends on.
 */
@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Detect the scripting backend of a Unity player")
@Severity(SeverityLevel.CRITICAL)
@Solution("UnityPlayerProjects/SimpleUnityGame")
@RiderTestTimeout(10, TimeUnit.MINUTES)
abstract class UnityScriptingBackendDetectionTestBase(
    private val expectedBackend: UnityScriptingBackend,
) : UnityPlayerTestBase() {

    /**
     * The detector must report the backend that the player was built with.
     *
     * A macOS player is an application bundle, so a run configuration can name the bundle instead of
     * the executable inside it. The detector must read the same backend from either path.
     */
    @Test
    @ChecklistItems(["Detect the scripting backend of a prebuilt Unity Player"])
    fun detectScriptingBackend() {
        val playerFile = getPlayerFile()
        assertEquals(expectedBackend, detectBackend(playerFile),
                     "Wrong scripting backend for the player at $playerFile")

        if (OS.CURRENT == OS.macOS) {
            val appBundle = generateSequence(playerFile) { it.parent }.first { it.name.endsWith(".app") }
            assertEquals(expectedBackend, detectBackend(appBundle),
                         "Wrong scripting backend for the application bundle at $appBundle")
        }
    }

    /**
     * Rider must warn about the IL2CPP limitation when the user starts an IL2CPP player under the
     * debugger, and it must stay silent for a Mono player.
     *
     * The test does not assert that the debugger attaches. An IL2CPP player carries no debugger agent,
     * so the session cannot bind, and the warning is the point.
     */
    @Test
    @ChecklistItems(["Warn that an IL2CPP player supports attach only"])
    fun warnAboutIl2cppDebugLimitation() {
        val playerFile = getPlayerFile()
        val seen = CopyOnWriteArrayList<Notification>()
        var warned = false
        project.messageBus.connect(testLifetime.createNestedDisposable()).subscribe(Notifications.TOPIC, object : Notifications {
            override fun notify(notification: Notification) {
                frameworkLogger.info("Notification: [${notification.type}] ${notification.content}")
                seen.add(notification)
            }
        })

        // Start of a run configuration starts the AppCode device monitor subprocess. Its output
        // reader outlives the test. isWellKnownOffender matches a thread name by `contains`.
        ThreadLeakTracker.longRunningThreadCreated(ApplicationManager.getApplication(), "JBDeviceService")

        selectStandalonePlayerConfiguration(playerFile)
        val expectedMessage = UnityBundle.message("debugging.il2cpp.backend.only.possible.with.attach")

        debugProgram(
            project = project,
            goldStream = NullPrintStream,
            beforeRun = {},
            test = { warned = waitForNotification(seen, expectedMessage) },
            exitProcessAfterTest = true,
            timeout = SESSION_TIMEOUT)

        val report = "Notifications seen: ${seen.joinToString(", ") { "'${it.content}'" }}"
        if (expectedBackend == UnityScriptingBackend.IL2CPP) {
            assertTrue(warned, "Rider did not warn that an IL2CPP player supports attach only. $report")
        }
        else {
            assertFalse(warned, "Rider warned about IL2CPP for a $expectedBackend player. $report")
        }
    }

    private fun detectBackend(path: Path): UnityScriptingBackend = runBlocking {
        withContext(Dispatchers.Default) {
            UnityPlayerRuntimeDetector.getInstance(project).detect(path, isEditor = false)
        }
    }

    /**
     * Makes the Unity executable run configuration for [playerFile] the selected one. The debug
     * executor then reaches `UnityExeDebugProfileState.execute`, which raises the warning.
     */
    private fun selectStandalonePlayerConfiguration(playerFile: Path) {
        val runManager = RunManager.getInstance(project)
        val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityExeConfigurationType::class.java)
        val settings = runManager.createConfiguration(STANDALONE_PLAYER_CONFIGURATION_NAME, configurationType.factory)
        val configuration = settings.configuration as UnityExeConfiguration
        configuration.parameters.exePath = playerFile.absolutePathString()
        configuration.parameters.workingDirectory = playerFile.parent.absolutePathString()
        configuration.parameters.programParameters = "-batchMode"
        configuration.isEditor = false
        selectRunConfiguration(project, configuration)
    }

    private fun waitForNotification(seen: List<Notification>, message: String): Boolean {
        val deadline = System.currentTimeMillis() + NOTIFICATION_TIMEOUT.toMillis()
        while (System.currentTimeMillis() < deadline) {
            if (seen.any { it.content == message }) return true
            flushQueues()
            Thread.sleep(POLL_INTERVAL_MS)
        }
        return false
    }

    companion object {
        private const val STANDALONE_PLAYER_CONFIGURATION_NAME = "Standalone Player"
        private const val POLL_INTERVAL_MS = 200L
        private val NOTIFICATION_TIMEOUT: Duration = Duration.ofSeconds(60)
        private val SESSION_TIMEOUT: Duration = Duration.ofSeconds(120)
    }
}

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Tag(TeamCityTags.Plugins.Unity.Integration)
@Suppress("JUnitTestCaseWithNoTests")
class UnityScriptingBackendDetectionTest {
    @Nested
    @UnityTestSettings(unityVersion = UnityVersion.V6_3, unityBackend = UnityBackend.Il2CPP)
    inner class TestIL2CPPUnityBuild6_3 : UnityScriptingBackendDetectionTestBase(UnityScriptingBackend.IL2CPP)

    @Nested
    @UnityTestSettings(unityVersion = UnityVersion.V6_3, unityBackend = UnityBackend.Mono)
    inner class TestMonoUnityBuild6_3 : UnityScriptingBackendDetectionTestBase(UnityScriptingBackend.Mono)
}
