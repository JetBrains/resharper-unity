package com.jetbrains.rider.unity.test.cases.agents

import com.google.gson.JsonParser
import com.intellij.execution.process.ProcessOutput
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.ABSENT
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.PRESENT
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.UNREADABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpStore
import com.jetbrains.rider.plugins.unity.settings.agents.readMcpServersEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCodexSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpConfigureLocalCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpHeldIsStale
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpLocalClientListCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpManualSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpMergedStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpOwnedStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRestartNeeded
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRowSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpWriteCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpWriteConfirmed
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpWriteOutcome
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
 * Pins the write rules of the Unity MCP section.
 *
 * The class runs no process and it needs no application. Every table below is the specification of
 * one rule, so a reader learns the rule from the test.
 *
 * Every store of this section lives in the solution directory. So every case here asserts that the
 * command line names the solution, through `--local` or through the working directory.
 *
 * The Codex configure is the only write a tool still does. [UnityMcpJsonStoreTest] and
 * [UnityMcpCodexStoreTest] answer for every write that Rider does itself.
 *
 * [UnityMcpConfigureStandInTest] proves that these command lines start a process at all.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpWriteTest {
    private val unity = Path.of("/home/tester/.unity/bin/unity")
    private val solution = Path.of("/home/tester/projects/Platformer")

    @Test
    fun `the Codex configure writes the project store and skips the prompt`() {
        val command = unityMcpConfigureLocalCommandLine(unity, solution)

        assertEquals(unity.toString(), command.exePath)
        assertEquals(listOf("mcp", "configure", "codex", "--local", "--yes"), command.parametersList.list)
        assertEquals(solution.toString(), command.workDirectory?.path, "the working directory names the project")
    }

    @Test
    fun `the read asks the Unity CLI for the project-local client list`() {
        val command = unityMcpLocalClientListCommandLine(unity, solution)

        assertEquals(listOf("mcp", "configure", "--list", "--local", "--json"), command.parametersList.list)
        assertEquals(solution.toString(), command.workDirectory?.path)
    }

    /**
     * Rider writes every JSON store itself, and it removes the Codex entry itself, so a null here is
     * not a missing input.
     *
     * [UnityMcpJsonStoreTest] and [UnityMcpCodexStoreTest] answer for these writes.
     */
    @Test
    fun `a write that Rider does itself has no command line`() {
        val owned = listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI, UnityMcpStore.JUNIE_CLI)
        for (store in owned) {
            for (operation in UnityMcpOperation.entries) {
                assertNull(
                    unityMcpWriteCommandLine(store, operation, unity, solution),
                    "$operation the $store store built a command line",
                )
            }
        }
        assertNull(unityMcpWriteCommandLine(UnityMcpStore.CODEX_CLI, UnityMcpOperation.REMOVING, unity, solution))
    }

    /**
     * No write may reach a store outside the solution.
     *
     * The working directory is what every one of them has in common. `claude mcp remove --scope
     * project` names the solution through that and through nothing else, because the scope word
     * means "the directory I am in".
     */
    @Test
    fun `every write runs in the solution directory`() {
        for (store in UnityMcpStore.entries) {
            for (operation in UnityMcpOperation.entries) {
                val command = unityMcpWriteCommandLine(store, operation, unity, solution) ?: continue
                assertEquals(
                    solution.toString(),
                    command.workDirectory?.path,
                    "$operation the $store store ran somewhere else",
                )
            }
        }
    }

    /** The delegated write also carries a marker that keeps it off the home directory of the user. */
    @Test
    fun `every delegated write carries its project-scope marker`() {
        val configure = unityMcpWriteCommandLine(UnityMcpStore.CODEX_CLI, UnityMcpOperation.CONFIGURING, unity, solution)
        assertTrue(
            configure?.parametersList?.list?.contains("--local") == true,
            "the Codex configure lost its '--local' marker",
        )
    }

    /**
     * The snippet is the only path to Unity MCP for a client with no row, so it must be pastable as
     * it stands. It names the Unity CLI absolutely and it pins the project, so it works from any
     * directory. It uses the server name of the stores, so a pasted copy reads "Configured".
     */
    @Test
    fun `the manual snippet holds the absolute command and the project pin`() {
        assertEquals(
            """
            {
              "unity-editor-mcp": {
                "command": "/home/tester/.unity/bin/unity",
                "args": [
                  "mcp",
                  "--project-path",
                  "/home/tester/projects/Platformer"
                ]
              }
            }
            """.trimIndent(),
            unityMcpManualSnippet(unity, solution),
        )
    }

    @Test
    fun `the manual snippet drops the pin when no project is open`() {
        assertEquals(
            """
            {
              "unity-editor-mcp": {
                "command": "/home/tester/.unity/bin/unity",
                "args": [
                  "mcp"
                ]
              }
            }
            """.trimIndent(),
            unityMcpManualSnippet(unity, null),
        )
    }

    @Test
    fun `the manual snippet escapes a Windows path`() {
        val snippet = unityMcpManualSnippet(Path.of("""C:\Users\tester\unity.exe"""), null)

        assertTrue(snippet.contains("""C:\\Users\\tester\\unity.exe"""), snippet)
    }

    /**
     * A pasted snippet must not add a second Unity server next to the one that Auto-Configure writes.
     */
    @Test
    fun `a pasted manual snippet reads as configured in a JSON store`() {
        val pasted = """{"mcpServers": ${unityMcpManualSnippet(unity, solution)}}"""

        assertEquals(PRESENT, readMcpServersEntry(pasted, fileExists = true))
    }

    @Test
    fun `the copy on a JSON row equals the entry Rider writes`() {
        for (store in UnityMcpStore.entries.filter { it != UnityMcpStore.CODEX_CLI }) {
            val written = unityMcpMergedStore(null, unityMcpOwnedStore(store, unity, solution)?.entry)
            assertEquals(
                JsonParser.parseString(written).asJsonObject.get("mcpServers"),
                JsonParser.parseString(unityMcpRowSnippet(store, unity)),
                "$store",
            )
        }
    }

    /**
     * The Codex store is TOML, so its row copies the four tables of `unity mcp configure codex`.
     */
    @Test
    fun `the copy on the Codex row is the TOML that the Unity CLI writes`() {
        assertEquals(
            """
            [mcp_servers.unity]
            command = "/home/tester/.unity/bin/unity"
            args = ["mcp"]

            [sandbox_workspace_write]
            network_access = true

            [features.network_proxy]
            enabled = true
            allow_local_binding = false

            [features.network_proxy.domains]
            localhost = "allow"
            "127.0.0.1" = "allow"
            "::1" = "allow"
            """.trimIndent(),
            unityMcpRowSnippet(UnityMcpStore.CODEX_CLI, unity),
        )
    }

    @Test
    fun `the Codex snippet keeps a quote and an ampersand literal`() {
        val snippet = unityMcpCodexSnippet(Path.of("/home/o'neil/R&D/unity"))

        assertTrue(snippet.contains("""command = "/home/o'neil/R&D/unity""""), snippet)
    }

    @Test
    fun `the Codex snippet escapes a Windows path`() {
        val snippet = unityMcpCodexSnippet(Path.of("""C:\Users\tester\unity.exe"""))

        assertTrue(snippet.contains("""command = "C:\\Users\\tester\\unity.exe""""), snippet)
    }

    private data class ConfirmCase(
        val name: String,
        val operation: UnityMcpOperation,
        val entry: UnityMcpEntry?,
        val expected: Boolean,
    )

    private val confirmCases = listOf(
        ConfirmCase("a configure that landed", UnityMcpOperation.CONFIGURING, PRESENT, true),
        ConfirmCase("a configure that wrote nothing", UnityMcpOperation.CONFIGURING, ABSENT, false),
        ConfirmCase("a configure that the re-read could not check", UnityMcpOperation.CONFIGURING, null, false),
        ConfirmCase("a removal that landed", UnityMcpOperation.REMOVING, ABSENT, true),
        ConfirmCase("a removal that left the entry", UnityMcpOperation.REMOVING, PRESENT, false),
        ConfirmCase("a removal that the re-read could not check", UnityMcpOperation.REMOVING, null, false),
    )

    @Test
    fun `the store says whether a write landed`() {
        for (case in confirmCases) {
            assertEquals(case.expected, unityMcpWriteConfirmed(case.operation, case.entry), case.name)
        }
    }

    private data class OutcomeCase(
        val name: String,
        val output: ProcessOutput?,
        val confirmed: Boolean,
        val expected: UnityMcpFailure?,
    )

    private val outcomeCases = listOf(
        OutcomeCase("the process did not start", null, false, UnityMcpFailure.LAUNCH_FAILED),
        OutcomeCase("the process ran past the timeout", timedOut(), true, UnityMcpFailure.LAUNCH_FAILED),
        OutcomeCase("the store holds what the write asked for", exit(0), true, null),
        // `claude mcp remove` returns 1 when the entry was already gone, measured on 2026-09-29.
        OutcomeCase("a non-zero exit over a store that agrees", exit(1), true, null),
        OutcomeCase("a non-zero exit over a store that did not change", exit(2), false, UnityMcpFailure.EXIT_CODE),
        // `codex mcp remove` returns 0 when it removed nothing, measured on 2026-09-29.
        OutcomeCase("a zero exit over a store that did not change", exit(0), false, UnityMcpFailure.NOT_CONFIRMED),
    )

    @Test
    fun `the store decides the outcome and the exit code only explains it`() {
        for (case in outcomeCases) {
            assertEquals(case.expected, unityMcpWriteOutcome(case.output, case.confirmed), case.name)
        }
    }

    private data class RestartCase(
        val name: String,
        val failure: UnityMcpFailure?,
        val before: UnityMcpEntry?,
        val after: UnityMcpEntry?,
        val heldRestart: Boolean,
        val heldEntry: UnityMcpEntry?,
        val expected: Boolean,
    )

    private val restartCases = listOf(
        RestartCase("a configure added the entry", null, ABSENT, PRESENT, false, null, true),
        RestartCase("a removal dropped the entry", null, PRESENT, ABSENT, false, null, true),
        RestartCase("a removal with nothing to remove", null, ABSENT, ABSENT, false, null, false),
        RestartCase("a configure over an entry that was there", null, PRESENT, PRESENT, false, null, false),
        RestartCase("a write that failed", UnityMcpFailure.FILE_WRITE, ABSENT, PRESENT, false, null, false),
        RestartCase("a read that gave no answer proves no change", null, UNREADABLE, PRESENT, false, null, false),
        RestartCase("a gated read proves no change", null, null, PRESENT, false, null, false),
        RestartCase("an earlier request over the file it left", null, PRESENT, PRESENT, true, PRESENT, true),
        RestartCase("an earlier request survives a failed write", UnityMcpFailure.EXIT_CODE, PRESENT, PRESENT, true, PRESENT, true),
        RestartCase("an earlier request over a file that changed since", null, ABSENT, ABSENT, true, PRESENT, false),
        RestartCase("an earlier request with no entry to check", null, PRESENT, PRESENT, true, null, false),
    )

    @Test
    fun `a write asks for a restart only when it moved the entry`() {
        for (case in restartCases) {
            assertEquals(
                case.expected,
                unityMcpRestartNeeded(case.failure, case.before, case.after, case.heldRestart, case.heldEntry),
                case.name,
            )
        }
    }

    private data class StaleCase(
        val name: String,
        val heldEntry: UnityMcpEntry?,
        val heldRelease: Long,
        val entry: UnityMcpEntry?,
        val readStart: Long,
        val expected: Boolean,
    )

    private val staleCases = listOf(
        StaleCase("the file still holds what the write left", PRESENT, 3, PRESENT, 5, false),
        StaleCase("the user fixed the file by hand", ABSENT, 3, PRESENT, 5, true),
        StaleCase("a checkout took the entry away", PRESENT, 3, ABSENT, 5, true),
        StaleCase("a read that began before the release", PRESENT, 6, ABSENT, 5, false),
        StaleCase("a read that began right after the release", PRESENT, 5, ABSENT, 5, true),
        StaleCase("a file that does not parse for a moment", PRESENT, 3, UNREADABLE, 5, false),
        StaleCase("a known entry clears an unreadable one", UNREADABLE, 3, ABSENT, 5, true),
        StaleCase("a gated read", PRESENT, 3, null, 5, false),
    )

    @Test
    fun `only a read that began after the release can drop it`() {
        for (case in staleCases) {
            assertEquals(
                case.expected,
                unityMcpHeldIsStale(case.heldEntry, case.heldRelease, case.entry, case.readStart),
                case.name,
            )
        }
    }

    private fun exit(code: Int): ProcessOutput = ProcessOutput("", "", code, false, false)

    private fun timedOut(): ProcessOutput = ProcessOutput("", "", -1, true, false)
}
