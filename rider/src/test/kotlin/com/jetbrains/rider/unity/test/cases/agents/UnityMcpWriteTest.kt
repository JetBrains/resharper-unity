package com.jetbrains.rider.unity.test.cases.agents

import com.google.gson.JsonParser
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.ABSENT
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.OTHER_CLI
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.PRESENT
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry.UNREADABLE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpStore
import com.jetbrains.rider.plugins.unity.settings.agents.readMcpServersEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCodexSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpHeldIsStale
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpManualSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpMergedStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpOwnedStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRestartNeeded
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRowSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpWriteConfirmed
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
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
 * Rider writes every store of this section itself. [UnityMcpJsonStoreTest] and [UnityMcpCodexStoreTest]
 * answer for those writes.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpWriteTest {
    private val unity = Path.of("/home/tester/.unity/bin/unity")
    private val solution = Path.of("/home/tester/projects/Platformer")

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
              "unity": {
                "command": "${escaped(unity)}",
                "args": [
                  "mcp",
                  "--project-path",
                  "${escaped(solution)}"
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
              "unity": {
                "command": "${escaped(unity)}",
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

        assertEquals(PRESENT, readMcpServersEntry(pasted, fileExists = true, unity = null))
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
     * The Codex store is TOML, so its row copies the four tables that Rider writes into it.
     */
    @Test
    fun `the copy on the Codex row is the TOML that Rider writes`() {
        assertEquals(
            """
            [mcp_servers.unity]
            command = "${escaped(unity)}"
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
        val path = Path.of("/home/o'neil/R&D/unity")
        val snippet = unityMcpCodexSnippet(path)

        assertTrue(snippet.contains("""command = "${escaped(path)}""""), snippet)
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
        RestartCase("a configure replaced the entry of a different Unity CLI", null, OTHER_CLI, PRESENT, false, null, true),
        RestartCase("a removal dropped the entry", null, PRESENT, ABSENT, false, null, true),
        RestartCase("a removal with nothing to remove", null, ABSENT, ABSENT, false, null, false),
        RestartCase("a configure over an entry that was there", null, PRESENT, PRESENT, false, null, false),
        RestartCase("a write that failed", UnityMcpFailure.FILE_WRITE, ABSENT, PRESENT, false, null, false),
        RestartCase("a read that gave no answer proves no change", null, UNREADABLE, PRESENT, false, null, false),
        RestartCase("a gated read proves no change", null, null, PRESENT, false, null, false),
        RestartCase("an earlier request over the file it left", null, PRESENT, PRESENT, true, PRESENT, true),
        RestartCase("an earlier request survives a failed write", UnityMcpFailure.FILE_WRITE, PRESENT, PRESENT, true, PRESENT, true),
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
        StaleCase("the detected Unity CLI moved", PRESENT, 3, OTHER_CLI, 5, true),
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

    /**
     * The text of [path] inside a JSON or a TOML string. Both formats escape a backslash, and Windows writes a backslash
     * between the parts of a path.
     */
    private fun escaped(path: Path): String = path.toString().replace("\\", "\\\\")
}
