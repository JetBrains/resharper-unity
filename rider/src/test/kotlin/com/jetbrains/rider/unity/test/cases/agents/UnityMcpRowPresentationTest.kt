package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_MCP_CODEX_KEY
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpRowPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpRestartLine
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpRowState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpStore
import com.jetbrains.rider.plugins.unity.settings.agents.configPathOf
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityMcpClientList
import com.jetbrains.rider.plugins.unity.settings.agents.readMcpServersEntry
import com.jetbrains.rider.plugins.unity.settings.agents.readCodexEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCanRemove
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpOpenableStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRestartLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpSectionState
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * Pins the model of the Unity MCP section.
 *
 * The class runs no process, it reads no file and it needs no application. Every table below is the
 * specification of one rule, so a reader learns the rule from the test.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpRowPresentationTest {
    private data class RowCase(val name: String, val row: UnityMcpRowPresentation, val expected: UnityMcpRowState)

    private data class GateCase(
        val name: String,
        val cli: UnityCliPresentation,
        val expected: UnityMcpSectionState,
        val unityProject: Boolean = true,
    )

    private val unity = Path.of("/home/tester/.unity/bin/unity")

    /** A row of a store that the section can reach, after the first read has returned. */
    private fun row(
        entry: UnityMcpEntry? = null,
        store: UnityMcpStore = UnityMcpStore.CODEX_CLI,
        section: UnityMcpSectionState = UnityMcpSectionState.READY,
        agentDetected: Boolean = true,
        restartNeeded: Boolean = false,
        operation: UnityMcpOperation? = null,
        failure: UnityMcpFailure? = null,
    ) = UnityMcpRowPresentation(
        store = store,
        section = section,
        entry = entry,
        agentDetected = agentDetected,
        restartNeeded = restartNeeded,
        operation = operation,
        failure = failure,
    )

    private val rowCases: List<RowCase> = listOf(
        RowCase(
            "a write runs on this row now",
            row(entry = UnityMcpEntry.ABSENT, operation = UnityMcpOperation.CONFIGURING),
            UnityMcpRowState.BUSY,
        ),
        RowCase(
            "a write outranks the failure of the write before it",
            row(operation = UnityMcpOperation.REMOVING, failure = UnityMcpFailure.EXIT_CODE),
            UnityMcpRowState.BUSY,
        ),
        RowCase(
            "the last write stopped on an exit code",
            row(entry = UnityMcpEntry.ABSENT, failure = UnityMcpFailure.EXIT_CODE),
            UnityMcpRowState.FAILED,
        ),
        RowCase(
            "a failure outranks the closed gate of the section",
            row(section = UnityMcpSectionState.NO_CLI, failure = UnityMcpFailure.LAUNCH_FAILED),
            UnityMcpRowState.FAILED,
        ),
        RowCase(
            "no Unity CLI is on the machine",
            row(entry = UnityMcpEntry.PRESENT, section = UnityMcpSectionState.NO_CLI),
            UnityMcpRowState.UNAVAILABLE,
        ),
        RowCase(
            "the project is not trusted",
            row(entry = UnityMcpEntry.PRESENT, section = UnityMcpSectionState.NOT_TRUSTED),
            UnityMcpRowState.UNAVAILABLE,
        ),
        RowCase(
            "the solution holds no Unity project",
            row(entry = UnityMcpEntry.PRESENT, section = UnityMcpSectionState.NO_PROJECT),
            UnityMcpRowState.UNAVAILABLE,
        ),
        // The gate outranks the first read, so the rows never flash a gate they do not have.
        RowCase(
            "a closed gate outranks a read that has not returned",
            row(entry = null, section = UnityMcpSectionState.NO_CLI),
            UnityMcpRowState.UNAVAILABLE,
        ),
        RowCase(
            "the first detection of the Unity CLI has not returned",
            row(entry = UnityMcpEntry.PRESENT, section = UnityMcpSectionState.CHECKING),
            UnityMcpRowState.CHECKING,
        ),
        RowCase(
            "the gate is open and the first read of the store has not returned",
            row(entry = null),
            UnityMcpRowState.CHECKING,
        ),
        RowCase(
            "the read of the store returned no answer",
            row(entry = UnityMcpEntry.UNREADABLE),
            UnityMcpRowState.UNREADABLE,
        ),
        RowCase(
            "a failure outranks a store that Rider cannot read",
            row(entry = UnityMcpEntry.UNREADABLE, failure = UnityMcpFailure.FILE_WRITE),
            UnityMcpRowState.FAILED,
        ),
        RowCase(
            "a store that Rider cannot read outranks the restart line",
            row(entry = UnityMcpEntry.UNREADABLE, restartNeeded = true),
            UnityMcpRowState.UNREADABLE,
        ),
        RowCase(
            "a write landed on this page",
            row(entry = UnityMcpEntry.PRESENT, restartNeeded = true),
            UnityMcpRowState.RESTART_NEEDED,
        ),
        // A removal also needs a restart, because the client keeps the server it already loaded.
        RowCase(
            "a removal landed on this page",
            row(entry = UnityMcpEntry.ABSENT, restartNeeded = true),
            UnityMcpRowState.RESTART_NEEDED,
        ),
        RowCase(
            "the store of the solution holds the entry",
            row(entry = UnityMcpEntry.PRESENT),
            UnityMcpRowState.ENTRY_PRESENT,
        ),
        // Configuration outranks presence. An entry can be there with the agent uninstalled.
        RowCase(
            "the entry is there and the agent is not on the machine",
            row(entry = UnityMcpEntry.PRESENT, agentDetected = false),
            UnityMcpRowState.ENTRY_PRESENT,
        ),
        RowCase(
            "no entry, and the probe found no agent",
            row(entry = UnityMcpEntry.ABSENT, agentDetected = false),
            UnityMcpRowState.NOT_DETECTED,
        ),
        RowCase(
            "the agent is on the machine and the store holds no entry",
            row(entry = UnityMcpEntry.ABSENT),
            UnityMcpRowState.NOT_CONFIGURED,
        ),
    )

    @Test
    fun eachRowProducesItsState() {
        for (case in rowCases) {
            assertEquals(case.expected, case.row.state, case.name)
        }
    }

    @Test
    fun everyRowStateHasACase() {
        assertEquals(
            UnityMcpRowState.entries.toSet(),
            rowCases.map { it.expected }.toSet(),
            "every row state needs one case",
        )
    }

    /**
     * The gate that no control of the page can open hides the body. Every other one keeps it.
     *
     * The dev ruled on 2026-09-29, after a dev Rider drew the whole disabled section under
     * "Open a Unity project to configure an agent.". See ticket 25 of the map of RIDER-143253.
     */
    /**
     * A removal that landed must not read "Configured".
     *
     * `RESTART_NEEDED` covers both writes, so the state alone cannot name one. The row carried the
     * configure sentence after a removal until the branch review found it, and the entry is the only
     * thing left that says which write landed.
     */
    /**
     * There is nothing to open before the first write.
     *
     * A JSON row names its file from the solution, so the path is there from the first read and the
     * file is not. The row offered "Open Configuration File" anyway, and the click did nothing.
     */
    @Test
    fun `a row offers Remove only while the store may hold the entry`() {
        assertTrue(unityMcpCanRemove(row(entry = UnityMcpEntry.PRESENT)))
        assertTrue(unityMcpCanRemove(row(entry = UnityMcpEntry.UNREADABLE)))
        assertTrue(unityMcpCanRemove(row(entry = null)))
        assertFalse(unityMcpCanRemove(row(entry = UnityMcpEntry.ABSENT)))
        assertFalse(unityMcpCanRemove(row(entry = UnityMcpEntry.ABSENT, restartNeeded = true)))
        assertFalse(unityMcpCanRemove(row(entry = UnityMcpEntry.PRESENT, operation = UnityMcpOperation.CONFIGURING)))
    }

    @Test
    fun `a row offers its store only once a file is there`() {
        assertEquals(
            "/Users/dev/projects/game/.mcp.json",
            unityMcpOpenableStore(row(entry = UnityMcpEntry.PRESENT).copy(
                storePath = "/Users/dev/projects/game/.mcp.json",
                storeFileExists = true,
            )),
            "the store is there, so the row opens it",
        )
        assertNull(
            unityMcpOpenableStore(row(entry = UnityMcpEntry.ABSENT).copy(
                storePath = "/Users/dev/projects/game/.mcp.json",
                storeFileExists = false,
            )),
            "the path names a file the first write creates, so there is nothing to open",
        )
    }

    @Test
    fun `the restart line names the write that landed`() {
        assertEquals(
            UnityMcpRestartLine.REMOVED,
            unityMcpRestartLine(UnityMcpEntry.ABSENT),
            "a removal landed, so the line says Removed",
        )
        assertEquals(
            UnityMcpRestartLine.CONFIGURED,
            unityMcpRestartLine(UnityMcpEntry.PRESENT),
            "a configure landed, so the line says Configured",
        )
        assertNotEquals(
            unityMcpRestartLine(UnityMcpEntry.PRESENT),
            unityMcpRestartLine(UnityMcpEntry.ABSENT),
            "and one sentence can never serve both",
        )
    }

    @Test
    fun `only a solution with no Unity project hides the body of the section`() {
        assertEquals(
            setOf(UnityMcpSectionState.NO_PROJECT),
            UnityMcpSectionState.entries.filterNot { it.drawsBody }.toSet(),
        )
    }

    private val gateCases: List<GateCase> = listOf(
        GateCase(
            "the first detection has not returned",
            UnityCliPresentation(),
            UnityMcpSectionState.CHECKING,
        ),
        // No Unity Editor means no server for an agent to reach, whatever the machine holds.
        GateCase(
            "the solution holds no Unity project",
            UnityCliPresentation(detected = true, location = unity),
            UnityMcpSectionState.NO_PROJECT,
            unityProject = false,
        ),
        GateCase(
            "no Unity project outranks an untrusted one",
            UnityCliPresentation(projectTrusted = false),
            UnityMcpSectionState.NO_PROJECT,
            unityProject = false,
        ),
        GateCase(
            "an untrusted project outranks an unfinished detection, because detection needs trust too",
            UnityCliPresentation(projectTrusted = false),
            UnityMcpSectionState.NOT_TRUSTED,
        ),
        GateCase(
            "an untrusted project with a Unity CLI still blocks",
            UnityCliPresentation(detected = true, location = unity, projectTrusted = false),
            UnityMcpSectionState.NOT_TRUSTED,
        ),
        GateCase(
            "detection finished and it found no Unity CLI",
            UnityCliPresentation(detected = true),
            UnityMcpSectionState.NO_CLI,
        ),
        // The gate reads `location` and never UnityCliOffer, where an install would report BUSY.
        GateCase(
            "an install runs now, so the section still has no path to write",
            UnityCliPresentation(detected = true, operation = UnityCliOperation.INSTALLING),
            UnityMcpSectionState.NO_CLI,
        ),
        GateCase(
            "a Unity CLI is on the machine",
            UnityCliPresentation(detected = true, installedVersion = "1.0.0-beta.11", location = unity),
            UnityMcpSectionState.READY,
        ),
        // An outdated copy configures an agent. Only the absence of a path closes the gate.
        GateCase(
            "an outdated Unity CLI opens the gate",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.9",
                location = unity,
                latestVersion = "1.0.0-beta.11",
            ),
            UnityMcpSectionState.READY,
        ),
    )

    @Test
    fun eachCliStateProducesItsGate() {
        for (case in gateCases) {
            assertEquals(case.expected, unityMcpSectionState(case.cli, case.unityProject), case.name)
        }
    }

    @Test
    fun everySectionStateHasACase() {
        assertEquals(
            UnityMcpSectionState.entries.toSet(),
            gateCases.map { it.expected }.toSet(),
            "every section state needs one case",
        )
    }

    /**
     * The Codex row reads the project-local client list of the Unity CLI.
     *
     * The four `status` values were measured on 2026-09-29 against `1.0.0-beta.11`. Only
     * `configured` means that the store holds the entry.
     */
    @Test
    fun theCodexReaderPinsEveryStatusValue() {
        assertEquals(UnityMcpEntry.PRESENT, readCodexEntry(clientList("configured")))
        assertEquals(UnityMcpEntry.ABSENT, readCodexEntry(clientList("not-configured")))
        assertEquals(UnityMcpEntry.ABSENT, readCodexEntry(clientList("file-not-found")))
        assertEquals(UnityMcpEntry.ABSENT, readCodexEntry(clientList("no-file")))
        assertEquals(UnityMcpEntry.UNREADABLE, readCodexEntry(null), "a list that Rider could not read is not an answer")
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            readCodexEntry(parseUnityMcpClientList(EMPTY_LIST)),
            "a list with no Codex client is not an answer",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            readCodexEntry(parseUnityMcpClientList("""{"success": true, "data": [{"key": "codex"}]}""")),
            "a Codex client with no status is not an answer",
        )
    }

    @Test
    fun `a null slot in the client list is dropped and the other clients still read`() {
        val list = parseUnityMcpClientList("""{"success": true, "data": [null, {"key": "codex", "status": "configured"}]}""")

        assertEquals(UnityMcpEntry.PRESENT, readCodexEntry(list))
    }

    @Test
    fun theClientListGivesTheCodexRowItsProjectPath() {
        val list = parseUnityMcpClientList(CLIENT_LIST)

        assertEquals("/home/tester/Platformer/.codex/config.toml", list?.configPathOf(UNITY_MCP_CODEX_KEY))
        assertNull(list?.configPathOf("claude-code"), "the Unity CLI keeps no path for a delegated write")
    }

    /**
     * Two rows read `<solution>/.mcp.json`, and the store is what decides their state.
     *
     * Two things outside the store may still differ, and both are honest. A click leaves its
     * failure on the row it ran on. And the probe answers for Claude Code and not for the GitHub
     * Copilot CLI, which has no measured binary name. So this case holds
     * `agentDetected` and walks every gate, every reading and the restart line.
     */
    @Test
    fun `the two rows over one file agree on every state the store decides`() {
        val shared = listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI)
        for (section in UnityMcpSectionState.entries) {
            for (entry in listOf(null) + UnityMcpEntry.entries) {
                for (restartNeeded in listOf(false, true)) {
                    val states = shared.map { store ->
                        row(entry = entry, store = store, section = section, restartNeeded = restartNeeded).state
                    }
                    assertEquals(
                        states.first(),
                        states.last(),
                        "the two rows over one file disagree on $section, $entry, restart=$restartNeeded",
                    )
                }
            }
        }
    }

    /**
     * The Claude Code row reads the project store of the solution.
     *
     * A missing file is an answer, and a file that Rider could not read is not. The reader must
     * fail safe, because the user owns this file and it can hold anything.
     */
    @Test
    fun theClaudeCodeReaderPinsEveryFileShape() {
        assertEquals(
            UnityMcpEntry.ABSENT,
            readMcpServersEntry(null, fileExists = false),
            "no file means no entry",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            readMcpServersEntry(null, fileExists = true),
            "a file Rider could not read is not an answer",
        )
        for (content in listOf("not json at all", "null", "[]", """{"mcpServers": []}""", """{"mcpServers": "a string"}""")) {
            assertEquals(
                UnityMcpEntry.UNREADABLE,
                readMcpServersEntry(content, fileExists = true),
                "'$content' is not an answer",
            )
        }
        for (content in listOf("", " \n", "{}", """{"mcpServers": null}""")) {
            assertEquals(UnityMcpEntry.ABSENT, readMcpServersEntry(content, fileExists = true), "'$content' holds no entry")
        }
        assertEquals(
            UnityMcpEntry.ABSENT,
            readMcpServersEntry("""{"mcpServers": {"other": {"command": "x"}}}""", fileExists = true),
            "another server is not the Unity entry",
        )
        assertEquals(
            UnityMcpEntry.PRESENT,
            readMcpServersEntry(CLAUDE_PROJECT_STORE, fileExists = true),
        )
    }

    private fun clientList(status: String) =
        parseUnityMcpClientList("""{"success": true, "data": [{"key": "codex", "status": "$status"}]}""")

    private companion object {
        const val EMPTY_LIST = """{"success": true, "data": []}"""

        const val CLIENT_LIST = """
            {
              "success": true,
              "command": "mcp-clients",
              "data": [
                {"key": "codex", "displayName": "OpenAI Codex CLI",
                 "configPath": "/home/tester/Platformer/.codex/config.toml", "status": "configured"},
                {"key": "claude-code", "displayName": "Claude Code CLI",
                 "configPath": null, "status": "no-file"}
              ],
              "errors": [],
              "warnings": []
            }
        """

        const val CLAUDE_PROJECT_STORE = """
            {
              "mcpServers": {
                "unity-editor-mcp": {
                  "type": "stdio",
                  "command": "/home/tester/.unity/bin/unity",
                  "args": ["mcp", "--project-path", "/home/tester/Platformer"],
                  "env": {}
                }
              }
            }
        """
    }
}
