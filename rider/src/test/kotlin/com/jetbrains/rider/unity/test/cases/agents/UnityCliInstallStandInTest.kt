package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.openapi.util.SystemInfo
import com.intellij.testFramework.common.timeoutRunBlocking
import com.intellij.testFramework.junit5.TestApplication
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CDN_BASE_URL
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallResult
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerRunner
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerScript
import com.jetbrains.rider.plugins.unity.settings.agents.runUnityCliInstall
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.Timeout
import org.junit.jupiter.api.io.TempDir
import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.exists
import kotlin.io.path.readText
import kotlin.time.Duration.Companion.seconds

/**
 * The install, with a real process instead of a stand-in for one.
 *
 * [UnityCliInstallRunTest] answers for the order of the steps, and every one of its cases hands
 * `runUnityCliInstall` a function in place of the process. So nothing there proves that the command
 * Rider builds runs at all. These cases keep the real
 * [UnityCliInstallerRunner][com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerRunner]
 * and give it a stand-in installer script that this test writes.
 *
 * No case runs the installer script of Unity, and none reaches the network. The download hands back
 * a local file, and the channel is forced.
 *
 * The stand-in script reports the channel it received, so the environment is read where it matters:
 * inside the process, and not off the command object.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityCliInstallStandInTest {
    @TempDir
    lateinit var directory: Path

    @Test
    @Timeout(60)
    fun `a script that exits 0 and leaves a copy is a working install`() {
        val result = install(exitCode = 0, leavesCli = true)

        assertNull(result.failure, "a working install reported a failure")
        assertEquals(0, result.exitCode)
    }

    @Test
    @Timeout(60)
    fun `a script that stops carries its own exit code out of the process`() {
        val result = install(exitCode = EXIT_CODE, leavesCli = false)

        assertEquals(UnityCliFailure.INSTALLER_EXIT_CODE, result.failure)
        assertEquals(EXIT_CODE, result.exitCode, "the exit code of the process did not reach the caller")
    }

    @Test
    @Timeout(60)
    fun `a script that exits 0 and leaves nothing fails the verification`() {
        val result = install(exitCode = 0, leavesCli = false)

        assertEquals(UnityCliFailure.VERIFICATION, result.failure)
    }

    @Test
    @Timeout(60)
    fun `the forced channel reaches the environment of the process`() {
        install(exitCode = 0, leavesCli = true)

        assertEquals(CHANNEL, channelFile.readText().trim(), "the script read another channel")
    }

    @Test
    @Timeout(60)
    fun `Rider deletes the script the process ran`() {
        val installer = writeStandIn(exitCode = 0, leavesCli = true)

        run(installer) { Files.exists(cliFile) }

        assertFalse(installer.exists(), "Rider left the installer script behind")
    }

    private fun install(exitCode: Int, leavesCli: Boolean): UnityCliInstallResult =
        run(writeStandIn(exitCode, leavesCli)) { Files.exists(cliFile) }

    private fun run(installer: Path, findCli: () -> Boolean): UnityCliInstallResult =
        timeoutRunBlocking(timeout = 45.seconds) {
            runUnityCliInstall(
                script = script,
                cdnBaseUrl = UNITY_CLI_CDN_BASE_URL,
                forcedChannel = CHANNEL,
                download = { installer },
                runInstaller = { UnityCliInstallerRunner.getInstance().runInstaller(it) {} },
                findCli = { findCli() },
            )
        }

    /** The paths are written into the body, because the command carries no argument. */
    private fun writeStandIn(exitCode: Int, leavesCli: Boolean): Path {
        val installer = Files.createTempFile(directory, "unity-cli-", "-${script.fileName}")
        val body = when (script) {
            UnityCliInstallerScript.SHELL -> """
                printf '%s' "${'$'}UNITY_CLI_CHANNEL" > '$channelFile'
                ${if (leavesCli) "printf 'unity' > '$cliFile'" else ":"}
                exit $exitCode
            """.trimIndent()

            UnityCliInstallerScript.POWERSHELL -> """
                Set-Content -Path '$channelFile' -Value ${'$'}env:UNITY_CLI_CHANNEL -NoNewline
                ${if (leavesCli) "Set-Content -Path '$cliFile' -Value 'unity' -NoNewline" else ""}
                exit $exitCode
            """.trimIndent()
        }
        Files.writeString(installer, body + System.lineSeparator())
        return installer
    }

    private val script: UnityCliInstallerScript
        get() = if (SystemInfo.isWindows) UnityCliInstallerScript.POWERSHELL else UnityCliInstallerScript.SHELL

    private val channelFile: Path get() = directory.resolve("channel.txt")

    private val cliFile: Path get() = directory.resolve("unity")

    private companion object {
        /** No meaning of its own. It is neither 0 nor 1, so no default can pass for it. */
        const val EXIT_CODE = 7

        /** Any value skips the probe of the CDN, so the case reaches no network. */
        const val CHANNEL = "beta"
    }
}
