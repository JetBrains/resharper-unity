package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.util.ExecUtil
import com.intellij.testFramework.junit5.TestApplication
import com.intellij.testFramework.junit5.http.localhostHttpServer
import com.intellij.testFramework.junit5.http.url
import com.jetbrains.rider.plugins.unity.settings.agents.BASH_ENV_VARIABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CDN_BASE_URL
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CDN_BASE_VARIABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CHANNEL_VARIABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerScript
import com.jetbrains.rider.plugins.unity.settings.agents.probeUnityCliChannel
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliCdnUrl
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliInstallOutcome
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliInstallerCommandLine
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.runBlocking
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Files
import java.nio.file.Path

/**
 * What Rider runs to install the Unity CLI, and what it calls a failed install.
 *
 * **No test here ever runs the installer script of Unity.** A human verifies that separately. These
 * cases pin the command line, the channel probe and the outcome rule, which is everything Rider
 * decides before the script takes over.
 *
 * The command line is asserted by its **parts** and never by `commandLineString`. That string cannot
 * see the environment, and both security controls of this feature live there: `UNITY_CLI_CHANNEL` on
 * a 404, and the scrub of `UNITY_CLI_CDN_BASE`.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityCliInstallTest {
    private val serverFixture = localhostHttpServer()
    private val server get() = serverFixture.get()

    private val installer: Path = Path.of("/tmp/unity-cli-1234-install.sh")

    @Test
    fun `the shell command runs bash on the file, and it is never interactive`() {
        val commandLine = unityCliInstallerCommandLine(UnityCliInstallerScript.SHELL, installer, null)

        assertEquals("bash", commandLine.exePath)
        assertEquals(listOf(installer.toString()), commandLine.parametersList.list)
        // `bash -i` reads the profile of the user, which could change what Rider runs. The installer
        // of the IDE CLI needs that profile. This script does not.
        assertTrue("-i" !in commandLine.parametersList.list, "the shell is interactive")
    }

    @Test
    fun `the PowerShell command reads no profile and stays non-interactive`() {
        val script = Path.of("C:\\Temp\\unity-cli-1234-install.ps1")

        val commandLine = unityCliInstallerCommandLine(UnityCliInstallerScript.POWERSHELL, script, null)

        assertEquals("powershell.exe", commandLine.exePath)
        assertEquals(
            listOf("-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script.toString()),
            commandLine.parametersList.list,
        )
    }

    @Test
    fun `every command scrubs the CDN base of the environment of the child`() {
        for (script in UnityCliInstallerScript.entries) {
            val commandLine = unityCliInstallerCommandLine(script, installer, null)

            // An empty value reads as unset, because install.sh expands the variable with `:-`.
            assertEquals("", commandLine.environment[UNITY_CLI_CDN_BASE_VARIABLE], script.name)
        }
    }

    @Test
    fun `every command scrubs BASH_ENV of the environment of the child`() {
        for (script in UnityCliInstallerScript.entries) {
            val commandLine = unityCliInstallerCommandLine(script, installer, null)

            // Bash reads this file before any script it runs, even a non-interactive one. An empty
            // value reads as unset, the same as the scrub of the CDN base above.
            assertEquals("", commandLine.environment[BASH_ENV_VARIABLE], script.name)
        }
    }

    @Test
    fun `Rider deletes the installer script once the process ends`() {
        val script = UnityCliInstallerScript.forCurrentOs() ?: return
        val installerFile = Files.createTempFile("unity-cli-test-", "-${script.fileName}")
        Files.writeString(installerFile, "exit 0" + System.lineSeparator())
        assertTrue(Files.exists(installerFile), "the throwaway script is missing before the run")

        val commandLine = unityCliInstallerCommandLine(script, installerFile, null)
        ExecUtil.execAndGetOutput(commandLine)

        assertFalse(Files.exists(installerFile), "Rider left the installer script behind")
    }

    @Test
    fun `a named channel reaches the environment, and no channel leaves it out`() {
        val beta = unityCliInstallerCommandLine(UnityCliInstallerScript.SHELL, installer, "beta")
        val stable = unityCliInstallerCommandLine(UnityCliInstallerScript.SHELL, installer, null)

        assertEquals("beta", beta.environment[UNITY_CLI_CHANNEL_VARIABLE])
        assertNull(stable.environment[UNITY_CLI_CHANNEL_VARIABLE], "a stable install named a channel")
    }

    @Test
    fun `a stable manifest that answers 200 asks for no channel`() {
        server.createContext("/latest.json") { exchange ->
            val body = """{"version": "1.0.0"}""".toByteArray()
            exchange.sendResponseHeaders(200, body.size.toLong())
            exchange.responseBody.write(body)
            exchange.close()
        }

        val channel = runBlocking { probeUnityCliChannel("${server.url}/", forcedChannel = "") }

        assertNull(channel, "the probe named a channel while the stable one answers")
    }

    @Test
    fun `a stable manifest that answers 404 asks for beta`() {
        // This is what the CDN of Unity does today, so an install with no channel fails. The probe
        // is why Rider needs no release to follow the stable one.
        server.createContext("/latest.json") { exchange ->
            exchange.sendResponseHeaders(404, -1)
            exchange.close()
        }

        val channel = runBlocking { probeUnityCliChannel("${server.url}/", forcedChannel = "") }

        assertEquals("beta", channel)
    }

    @Test
    fun `a forced channel skips the probe`() {
        // No context is registered on the server, so a probe would answer 404 and ask for beta.
        val alpha = runBlocking { probeUnityCliChannel("${server.url}/", forcedChannel = "alpha") }
        val stable = runBlocking { probeUnityCliChannel("${server.url}/", forcedChannel = "stable") }

        assertEquals("alpha", alpha)
        assertNull(stable, "the value stable named a channel")
    }

    @Test
    fun `a non-zero exit code is a stopped installer`() {
        assertEquals(UnityCliFailure.INSTALLER_EXIT_CODE, unityCliInstallOutcome(exitCode = 1, cliFound = false))
        assertEquals(UnityCliFailure.INSTALLER_EXIT_CODE, unityCliInstallOutcome(exitCode = 1, cliFound = true))
    }

    @Test
    fun `an installer that never started names no exit code`() {
        assertEquals(UnityCliFailure.LAUNCH_FAILED, unityCliInstallOutcome(exitCode = null, cliFound = false))
        // The verification never runs, because there is nothing to verify.
        assertEquals(UnityCliFailure.LAUNCH_FAILED, unityCliInstallOutcome(exitCode = null, cliFound = true))
    }

    @Test
    fun `an exit code of zero with nothing to find is a failed verification`() {
        assertEquals(UnityCliFailure.VERIFICATION, unityCliInstallOutcome(exitCode = 0, cliFound = false))
    }

    @Test
    fun `an exit code of zero with a copy to find is no failure`() {
        assertNull(unityCliInstallOutcome(exitCode = 0, cliFound = true))
    }

    @Test
    fun `the base of the CDN joins one slash, whether it carries one or not`() {
        assertEquals(
            "https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh",
            unityCliCdnUrl(UNITY_CLI_CDN_BASE_URL, "install.sh"),
        )
        assertEquals("https://host/x/install.ps1", unityCliCdnUrl("https://host/x", "install.ps1"))
    }
}
