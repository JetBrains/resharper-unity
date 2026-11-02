package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.UnityBundle
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
import com.jetbrains.rider.plugins.unity.settings.agents.UnityOldEditor
import com.jetbrains.rider.plugins.unity.settings.agents.readMcpServersEntry
import com.jetbrains.rider.plugins.unity.settings.agents.readCodexEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCanConfigure
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCanRemove
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpGateMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpOpenableStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRestartLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpRowStatus
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
        restartNeeded: Boolean = false,
        operation: UnityMcpOperation? = null,
        failure: UnityMcpFailure? = null,
    ) = UnityMcpRowPresentation(
        store = store,
        section = section,
        entry = entry,
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
            row(operation = UnityMcpOperation.REMOVING, failure = UnityMcpFailure.FILE_WRITE),
            UnityMcpRowState.BUSY,
        ),
        RowCase(
            "the last write could not write the store",
            row(entry = UnityMcpEntry.ABSENT, failure = UnityMcpFailure.FILE_WRITE),
            UnityMcpRowState.FAILED,
        ),
        RowCase(
            "a failure outranks the closed gate of the section",
            row(section = UnityMcpSectionState.NO_CLI, failure = UnityMcpFailure.PACKAGE_INSTALL),
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
        RowCase(
            "the store holds the entry of a different Unity CLI",
            row(entry = UnityMcpEntry.OTHER_CLI, store = UnityMcpStore.CLAUDE_CODE_CLI),
            UnityMcpRowState.OTHER_CLI,
        ),
        RowCase(
            "the store holds no entry",
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

    @Test
    fun `a Codex store that Rider cannot edit fails its row with a problem text`() {
        val row = row(entry = UnityMcpEntry.UNREADABLE, failure = UnityMcpFailure.CODEX_STORE_SHAPE)

        assertEquals(UnityMcpRowState.FAILED, row.state)
        val status = unityMcpRowStatus(row)
        assertTrue(status.isProblem)
        assertEquals(UnityBundle.message("unity.agents.mcp.failed.codex.shape"), status.text)
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

    /** Found in a manual run on 2026-10-08: the click on a broken JSON store wrote nothing and blamed the write. */
    @Test
    fun `a broken JSON store cannot be configured, and the Codex row still can`() {
        for (store in listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI, UnityMcpStore.JUNIE_CLI)) {
            assertFalse(unityMcpCanConfigure(row(entry = UnityMcpEntry.UNREADABLE, store = store)), "$store")
            assertTrue(unityMcpCanConfigure(row(entry = UnityMcpEntry.ABSENT, store = store)), "$store")
        }
        assertTrue(
            unityMcpCanConfigure(row(entry = UnityMcpEntry.UNREADABLE, store = UnityMcpStore.CODEX_CLI)),
            "the Codex row reads through the Unity CLI, and its write can still land",
        )
        assertFalse(unityMcpCanConfigure(row(entry = UnityMcpEntry.ABSENT, operation = UnityMcpOperation.CONFIGURING)))
    }

    /** RIDER-144470: Rider does not look for the agent, so a row can be configured for an agent that Rider cannot find. */
    @Test
    fun `every row without an entry can be configured`() {
        for (store in UnityMcpStore.entries) {
            assertTrue(unityMcpCanConfigure(row(entry = UnityMcpEntry.ABSENT, store = store)), "$store")
        }
    }

    /** RIDER-144088: Auto-Configure replaces the entry of a different Unity CLI, and Remove drops it. */
    @Test
    fun `a row with the entry of a different Unity CLI can be configured and removed`() {
        for (store in listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI, UnityMcpStore.JUNIE_CLI)) {
            val row = row(entry = UnityMcpEntry.OTHER_CLI, store = store)
            assertTrue(unityMcpCanConfigure(row), "$store")
            assertTrue(unityMcpCanRemove(row), "$store")
        }
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

    @Test
    fun `the gate message asks only for the steps that are missing`() {
        assertEquals(
            UnityBundle.message("unity.agents.mcp.needs.setup"),
            unityMcpGateMessage(UnityMcpSectionState.NO_CLI, packageMissing = true),
        )
        assertEquals(
            UnityBundle.message("unity.agents.mcp.needs.cli"),
            unityMcpGateMessage(UnityMcpSectionState.NO_CLI, packageMissing = false),
        )
        assertEquals(
            UnityBundle.message("unity.agents.mcp.needs.package"),
            unityMcpGateMessage(UnityMcpSectionState.READY, packageMissing = true),
        )
        assertNull(unityMcpGateMessage(UnityMcpSectionState.READY, packageMissing = false))
        assertNull(unityMcpGateMessage(UnityMcpSectionState.CHECKING, packageMissing = true))
    }

    @Test
    fun `the gate message names the old Editor when it blocks the package`() {
        val oldEditor = UnityOldEditor("6000.0", "2022.3.62f1")
        val oldEditorText = UnityBundle.message("unity.agents.old.editor.gate", "6000.0", "2022.3.62f1")
        assertEquals(
            oldEditorText,
            unityMcpGateMessage(UnityMcpSectionState.NO_CLI, packageMissing = true, blockingEditor = oldEditor),
        )
        assertEquals(
            oldEditorText,
            unityMcpGateMessage(UnityMcpSectionState.READY, packageMissing = true, blockingEditor = oldEditor),
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
     * The Codex row reads the `[mcp_servers.unity]` table of `<solution>/.codex/config.toml`.
     *
     * A missing file is an answer, and a file that Rider could not read is not. A store with no Unity table holds no
     * entry, and a header that does not parse is not an answer.
     */
    @Test
    fun theCodexReaderPinsEveryFileShape() {
        assertEquals(
            UnityMcpEntry.ABSENT,
            readCodexEntry(null, fileExists = false, unity = unity),
            "no file means no entry",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            readCodexEntry(null, fileExists = true, unity = unity),
            "a file Rider could not read is not an answer",
        )
        assertEquals(
            UnityMcpEntry.ABSENT,
            codexEntry("[sandbox_workspace_write]\nnetwork_access = true\n"),
            "a store with no Unity table holds no entry",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            codexEntry("[mcp_servers.unity\ncommand = \"/home/tester/.unity/bin/unity\"\n"),
            "a header that does not parse is not an answer",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            codexEntry("mcp_servers = { unity = { command = \"unity\", args = [\"mcp\"] } }\n"),
            "an inline mcp_servers table is not an answer",
        )
    }

    /** RIDER-144088: a configured entry can run another Unity CLI, so the reader compares the command of the store. */
    @Test
    fun `the Codex reader compares the command of the entry with the detected Unity CLI`() {
        assertEquals(UnityMcpEntry.PRESENT, codexEntry(codexStore("/home/tester/.unity/bin/unity")))
        assertEquals(UnityMcpEntry.OTHER_CLI, codexEntry(codexStore("/opt/homebrew/bin/unity")))
        assertEquals(
            UnityMcpEntry.PRESENT,
            codexEntry(codexStore("unity")),
            "a bare command runs the Unity CLI from the PATH",
        )
        assertEquals(UnityMcpEntry.PRESENT, codexEntry(codexStore("unity.exe")))
        assertEquals(
            UnityMcpEntry.ABSENT,
            codexEntry(codexStore("unity123")),
            "a bare command of another program is another MCP server",
        )
    }

    /** RIDER-144843: the issue shows this server of MCP for Unity under the name that Rider writes. */
    @Test
    fun `the Codex reader takes another server named unity as no entry`() {
        val otherServer = "[mcp_servers.unity]\ncommand = \"uvx\"\nargs = [\"mcp-for-unity\"]\n"

        assertEquals(UnityMcpEntry.ABSENT, codexEntry(otherServer))
        assertEquals(UnityMcpEntry.ABSENT, codexEntry(codexStore("/usr/local/bin/uvx")))
        assertEquals(UnityMcpEntry.ABSENT, readCodexEntry(otherServer, fileExists = true, unity = null))
        assertEquals(
            UnityMcpEntry.ABSENT,
            codexEntry("[mcp_servers.unity]\nurl = \"http://localhost:8080/mcp\"\n"),
            "an HTTP server has no command",
        )
        assertEquals(
            UnityMcpEntry.ABSENT,
            codexEntry("[mcp_servers.unity]\nargs = [\"mcp\"]\n"),
            "an entry with no command runs no Unity CLI",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            codexEntry("[mcp_servers]\nunity = { command = \"uvx\", args = [\"mcp-for-unity\"] }\n"),
            "an inline unity table is not an answer",
        )
        assertEquals(
            UnityMcpEntry.ABSENT,
            codexEntry("[mcp_servers.unity]\ncommand = \"uvx\"\n\n[features]\nnetwork_proxy.enabled = true\n"),
            "a store that Rider cannot edit still holds another server",
        )
    }

    /** On Windows, the PATH lookup can find a wrapper such as `unity.cmd`. The entry that runs the detected Unity CLI is the entry. */
    @Test
    fun `the Codex reader takes the detected Unity CLI as the entry under any file name`() {
        val wrapper = Path.of("/home/tester/bin/unity.cmd")
        val store = codexStore("/home/tester/bin/unity.cmd")

        assertEquals(UnityMcpEntry.PRESENT, readCodexEntry(store, fileExists = true, unity = wrapper))
        assertEquals(UnityMcpEntry.ABSENT, codexEntry(store), "a wrapper of another Unity CLI is another program")
    }

    @Test
    fun `the Codex reader reads a dotted key and a command it cannot read as the entry`() {
        assertEquals(
            UnityMcpEntry.PRESENT,
            codexEntry("[mcp_servers.unity]\ncommand = \"\"\"/home/tester/.unity/bin/unity\"\"\"\n"),
            "a command that is not a one-line string is still the entry",
        )
        assertEquals(
            UnityMcpEntry.PRESENT,
            codexEntry("mcp_servers.unity.command = \"/home/tester/.unity/bin/unity\"\n"),
            "a dotted key under the Unity table is the entry",
        )
        assertEquals(UnityMcpEntry.OTHER_CLI, codexEntry("mcp_servers.unity.command = \"/opt/homebrew/bin/unity\"\n"))
    }

    @Test
    fun `a read with no Unity CLI takes any entry as present`() {
        assertEquals(
            UnityMcpEntry.PRESENT,
            readCodexEntry(codexStore("/opt/homebrew/bin/unity"), fileExists = true, unity = null),
            "a read with no Unity CLI takes any one",
        )
    }

    /**
     * Two rows read `<solution>/.mcp.json`, and the store is what decides their state.
     *
     * One thing outside the store may still differ, and it is honest: a click leaves its failure on
     * the row it ran on. So this case walks every gate, every reading and the restart line.
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
            readMcpServersEntry(null, fileExists = false, unity = null),
            "no file means no entry",
        )
        assertEquals(
            UnityMcpEntry.UNREADABLE,
            readMcpServersEntry(null, fileExists = true, unity = null),
            "a file Rider could not read is not an answer",
        )
        for (content in listOf("not json at all", "null", "[]", """{"mcpServers": []}""", """{"mcpServers": "a string"}""")) {
            assertEquals(
                UnityMcpEntry.UNREADABLE,
                readMcpServersEntry(content, fileExists = true, unity = null),
                "'$content' is not an answer",
            )
        }
        for (content in listOf("", " \n", "{}", """{"mcpServers": null}""")) {
            assertEquals(UnityMcpEntry.ABSENT, readMcpServersEntry(content, fileExists = true, unity = null), "'$content' holds no entry")
        }
        assertEquals(
            UnityMcpEntry.ABSENT,
            readMcpServersEntry("""{"mcpServers": {"other": {"command": "x"}}}""", fileExists = true, unity = null),
            "another server is not the Unity entry",
        )
        assertEquals(
            UnityMcpEntry.PRESENT,
            readMcpServersEntry(CLAUDE_PROJECT_STORE, fileExists = true, unity = null),
        )
    }

    /** Reads [content] as the Codex store of the solution, which exists. */
    private fun codexEntry(content: String): UnityMcpEntry = readCodexEntry(content, fileExists = true, unity = unity)

    private fun codexStore(command: String) = "[mcp_servers.unity]\ncommand = \"$command\"\nargs = [\"mcp\"]\n"

    private companion object {
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
