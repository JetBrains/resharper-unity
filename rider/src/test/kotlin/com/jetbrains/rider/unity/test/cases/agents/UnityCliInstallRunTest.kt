package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CHANNEL_VARIABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallResult
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerScript
import com.jetbrains.rider.plugins.unity.settings.agents.runUnityCliInstall
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.runBlocking
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertThrows
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * The order of the install, and what each step does to the outcome.
 *
 * **No case runs the installer script of Unity, and none starts a process.** Every call to the
 * outside world is a parameter of [runUnityCliInstall], so the branches run here with no project, no
 * network and no application. A human verifies the real script separately.
 *
 * The channel is forced in every case, so no case reaches the CDN. [UnityCliInstallTest] pins the
 * probe itself.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityCliInstallRunTest {
    private val installer: Path = Path.of("/tmp/unity-cli-1234-install.sh")

    @Test
    fun `a stopped installer carries its exit code out`() {
        val result = install(exitCode = 3, cliFound = false)

        assertEquals(UnityCliFailure.INSTALLER_EXIT_CODE, result.failure)
        assertEquals(3, result.exitCode, "the exit code did not reach the caller")
    }

    @Test
    fun `an installer that exits 0 and leaves nothing is a failed verification`() {
        val result = install(exitCode = 0, cliFound = false)

        assertEquals(UnityCliFailure.VERIFICATION, result.failure)
    }

    @Test
    fun `an installer that exits 0 and leaves a copy is no failure`() {
        val result = install(exitCode = 0, cliFound = true)

        assertNull(result.failure, "a working install reported a failure")
    }

    @Test
    fun `a failed download stops before the installer runs`() {
        var ran = false

        // The contract of the parameter: null for a failed download, and an exception for a cancel.
        val result = install(exitCode = 0, cliFound = true, download = { null }) { ran = true }

        assertEquals(UnityCliFailure.DOWNLOAD, result.failure)
        assertNull(result.exitCode, "a failed download named an exit code")
        assertFalse(ran, "a failed download still ran the installer")
    }

    @Test
    fun `a cancelled download reports nothing, and it runs nothing`() {
        var ran = false

        // The exception leaves the whole install, so the caller never records a failure.
        assertThrows(CancellationException::class.java) {
            install(
                exitCode = 0,
                cliFound = true,
                download = { throw CancellationException("the user pressed Cancel") },
            ) { ran = true }
        }

        assertFalse(ran, "a cancelled download still ran the installer")
    }

    @Test
    fun `a cancelled installer reports nothing`() {
        assertThrows(CancellationException::class.java) {
            runBlocking {
                runUnityCliInstall(
                    script = UnityCliInstallerScript.SHELL,
                    cdnBaseUrl = "https://cdn.example/cli/",
                    forcedChannel = "beta",
                    download = { installer },
                    runInstaller = { throw CancellationException("the user closed the console") },
                    findCli = { false },
                )
            }
        }
    }

    @Test
    fun `the download reads the script of the operating system from the base of the CDN`() {
        val urls = mutableListOf<String>()

        for (script in UnityCliInstallerScript.entries) {
            runBlocking {
                runUnityCliInstall(
                    script = script,
                    cdnBaseUrl = "https://cdn.example/cli/",
                    forcedChannel = "beta",
                    download = { url -> urls.add(url); installer },
                    runInstaller = { 0 },
                    findCli = { true },
                )
            }
        }

        assertEquals(
            listOf("https://cdn.example/cli/install.sh", "https://cdn.example/cli/install.ps1"),
            urls,
        )
    }

    @Test
    fun `the forced channel reaches the command of the installer`() {
        var commandLine: GeneralCommandLine? = null

        runBlocking {
            runUnityCliInstall(
                script = UnityCliInstallerScript.SHELL,
                cdnBaseUrl = "https://cdn.example/cli/",
                forcedChannel = "alpha",
                download = { installer },
                runInstaller = { command -> commandLine = command; 0 },
                findCli = { true },
            )
        }

        assertTrue(commandLine != null, "the installer never ran")
        assertEquals("alpha", commandLine?.environment?.get(UNITY_CLI_CHANNEL_VARIABLE))
    }

    /** One install, with a forced channel so that no case reaches the network. */
    private fun install(
        exitCode: Int,
        cliFound: Boolean,
        download: suspend (String) -> Path? = { installer },
        onRun: () -> Unit = {},
    ): UnityCliInstallResult = runBlocking {
        runUnityCliInstall(
            script = UnityCliInstallerScript.SHELL,
            cdnBaseUrl = "https://cdn.example/cli/",
            forcedChannel = "beta",
            download = download,
            runInstaller = { onRun(); exitCode },
            findCli = { cliFound },
        )
    }
}
