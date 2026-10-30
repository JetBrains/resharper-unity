package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CDN_BASE_VARIABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallResult
import com.jetbrains.rider.plugins.unity.settings.agents.runUnityCliUpdateChain
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateCommandForClipboard
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateOutcome
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.runBlocking
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
 * The command line is asserted by its **parts**, because the joined string cannot show the
 * environment. These cases are also the positive control for the no-elevation claim: no `sudo`
 * reaches any command line.
 *
 * The chain cases run [runUnityCliUpdateChain] with a fake command runner and a fake CLI, so no real
 * command runs.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityCliUpdateTest {
    @Test
    fun `Homebrew runs a fixed argv`() {
        val commandLine = unityCliUpdateCommandLine(UnityCliInstallMethod.HOMEBREW)

        assertEquals("brew", commandLine?.exePath)
        assertEquals(listOf("upgrade", "unity-cli"), commandLine?.parametersList?.list)
    }

    @Test
    fun `winget runs a fixed argv`() {
        val commandLine = unityCliUpdateCommandLine(UnityCliInstallMethod.WINGET)

        assertEquals("winget", commandLine?.exePath)
        assertEquals(listOf("upgrade", "Unity.CLI"), commandLine?.parametersList?.list)
    }

    @Test
    fun `CDN, apt, dnf and unknown copies build no package manager command`() {
        // The CDN copy updates through the chain, and apt and dnf need sudo, so Rider runs nothing here.
        assertNull(unityCliUpdateCommandLine(UnityCliInstallMethod.CDN), "Rider built a package manager command for CDN")
        assertNull(unityCliUpdateCommandLine(UnityCliInstallMethod.APT), "Rider built a command line for apt")
        assertNull(unityCliUpdateCommandLine(UnityCliInstallMethod.DNF), "Rider built a command line for dnf")
        assertNull(unityCliUpdateCommandLine(UnityCliInstallMethod.UNKNOWN))
    }

    @Test
    fun `no command line elevates, on any method Rider owns`() {
        val chain = runChain(versions = listOf(OLD_VERSION, LATEST_VERSION))
        val commandLines: List<GeneralCommandLine> = listOfNotNull(
            unityCliUpdateCommandLine(UnityCliInstallMethod.HOMEBREW),
            unityCliUpdateCommandLine(UnityCliInstallMethod.WINGET),
        ) + chain.commands

        assertEquals(2, chain.commands.size, "the chain did not run all of its commands")
        for (commandLine in commandLines) {
            val parts = listOf(commandLine.exePath) + commandLine.parametersList.list
            assertTrue("sudo" !in parts, "a command line elevates: $parts")
            assertTrue(parts.none { it.endsWith("/sudo") }, "a command line elevates: $parts")
        }
    }

    // --- unityCliUpdateOutcome: the result of one update command ---

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

    // --- unityCliUpdateCommandForClipboard: the fixed command for the methods that need sudo ---

    @Test
    fun `apt shows the fixed clipboard command`() {
        assertEquals("sudo apt-get install --only-upgrade unity-cli", unityCliUpdateCommandForClipboard(UnityCliInstallMethod.APT))
    }

    @Test
    fun `dnf shows the fixed clipboard command`() {
        assertEquals("sudo dnf upgrade unity-cli", unityCliUpdateCommandForClipboard(UnityCliInstallMethod.DNF))
    }

    @Test
    fun `a method without a clipboard command shows nothing on the clipboard`() {
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.CDN))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.HOMEBREW))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.WINGET))
        assertNull(unityCliUpdateCommandForClipboard(UnityCliInstallMethod.UNKNOWN))
    }

    // --- runUnityCliUpdateChain: the CDN update, step by step ---

    @Test
    fun `a self-update that makes the copy current ends the chain`() {
        val run = runChain(versions = listOf(LATEST_VERSION))

        assertNull(run.result.failure, "a working self-update reported a failure")
        assertEquals(1, run.commands.size, "the chain ran more than the self-update")
        val selfUpdate = run.commands.single()
        assertEquals(CLI_PATH.toString(), selfUpdate.exePath)
        assertEquals(listOf("self-update", "-y"), selfUpdate.parametersList.list)
        // An empty value clears the CDN base, so a value of the user cannot redirect the update.
        assertEquals("", selfUpdate.environment[UNITY_CLI_CDN_BASE_VARIABLE])
        assertOnlyCliCommands(run.commands)
    }

    @Test
    fun `a self-update that leaves an old copy is followed by upgrade, which makes the copy current`() {
        val run = runChain(
            versions = listOf(OLD_VERSION, LATEST_VERSION),
            exitCodeOf = exitCodesOf(selfUpdate = SELF_UPDATE_EXIT_CODE, upgrade = 0),
        )

        assertNull(run.result.failure, "a repaired copy reported a failure")
        assertEquals(listOf(listOf("self-update", "-y"), listOf("upgrade", "-y")), run.commands.map { it.parametersList.list })
        assertEquals(0, run.result.exitCode, "the result did not carry the exit code of upgrade")
        assertOnlyCliCommands(run.commands)
    }

    @Test
    fun `a self-update that did not start is followed by upgrade, which makes the copy current`() {
        val run = runChain(
            versions = listOf(OLD_VERSION, LATEST_VERSION),
            exitCodeOf = exitCodesOf(selfUpdate = null, upgrade = 0),
        )

        assertNull(run.result.failure, "a repaired copy reported a failure")
        assertEquals(listOf(listOf("self-update", "-y"), listOf("upgrade", "-y")), run.commands.map { it.parametersList.list })
        assertOnlyCliCommands(run.commands)
    }

    @Test
    fun `an upgrade that leaves an old copy fails, and names its exit code`() {
        val run = runChain(
            versions = listOf(OLD_VERSION, OLD_VERSION),
            exitCodeOf = exitCodesOf(selfUpdate = SELF_UPDATE_EXIT_CODE, upgrade = UPGRADE_EXIT_CODE),
        )

        assertEquals(UnityCliFailure.UPDATE_FAILED, run.result.failure)
        assertEquals(UPGRADE_EXIT_CODE, run.result.exitCode)
        assertEquals(2, run.commands.size, "the chain did not stop after upgrade")
        assertOnlyCliCommands(run.commands)
    }

    @Test
    fun `no latest version stops the chain before anything runs`() {
        val run = runChain(versions = emptyList(), latest = null)

        assertEquals(UnityCliFailure.MANIFEST_READ, run.result.failure)
        assertTrue(run.commands.isEmpty(), "the chain ran a command without a latest version")
    }

    /** What one run of the update chain did, in the order it did it. */
    private class ChainRun(
        val result: UnityCliInstallResult,
        val commands: List<GeneralCommandLine>,
    )

    /** Asserts that the chain ran only the CLI, and never `self-uninstall`. */
    private fun assertOnlyCliCommands(commands: List<GeneralCommandLine>) {
        for (commandLine in commands) {
            assertEquals(CLI_PATH.toString(), commandLine.exePath, "the chain ran a program other than the CLI")
            assertTrue("self-uninstall" !in commandLine.parametersList.list, "the chain ran self-uninstall")
        }
    }

    /** Gives the exit code of `self-update` and of `upgrade`. Any other command exits with 0. */
    private fun exitCodesOf(selfUpdate: Int?, upgrade: Int?): (GeneralCommandLine) -> Int? = { commandLine ->
        when (commandLine.parametersList.list.firstOrNull()) {
            "self-update" -> selfUpdate
            "upgrade" -> upgrade
            else -> 0
        }
    }

    /** Runs the chain with a fake CLI. Each check of the installed version takes the next of [versions]. */
    private fun runChain(
        versions: List<String?>,
        latest: String? = LATEST_VERSION,
        exitCodeOf: (GeneralCommandLine) -> Int? = { 0 },
    ): ChainRun {
        val commands = mutableListOf<GeneralCommandLine>()
        val checks = ArrayDeque(versions)
        val result = runBlocking {
            runUnityCliUpdateChain(
                cliPath = CLI_PATH,
                readLatestVersion = { latest },
                runCommand = { commandLine ->
                    commands += commandLine
                    exitCodeOf(commandLine)
                },
                findCli = { checks.removeFirst() },
            )
        }
        return ChainRun(result, commands)
    }

    private companion object {
        val CLI_PATH: Path = Path.of("/opt/homebrew/bin/unity")
        const val OLD_VERSION = "1.0.0-beta.9"
        const val LATEST_VERSION = "1.0.0-beta.10"
        const val SELF_UPDATE_EXIT_CODE = 1
        const val UPGRADE_EXIT_CODE = 3
    }
}
