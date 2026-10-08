package com.jetbrains.rider.unity.test.cases.agents

import com.google.gson.JsonParser
import com.intellij.util.system.OS
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpServerEntry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpStore
import com.jetbrains.rider.plugins.unity.settings.agents.readMcpServersEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpAgentBinary
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpAgentWellKnownCandidates
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpFileWriteOutcome
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpJsonStoreFile
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpMergedStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpOwnedStore
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpStoreSharers
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpUnnamedSharers
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpWriteGroups
import com.jetbrains.rider.plugins.unity.settings.agents.writeUnityMcpJsonStore
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertThrows
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.io.TempDir
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.exists
import kotlin.io.path.readText
import kotlin.io.path.writeText

/**
 * Pins the stores that Rider writes itself.
 *
 * Rider writes every JSON store of the section. The Junie store has no owner at all, and scope call
 * 19 gave `<solution>/.mcp.json` to Rider as well, so that a Copilot row works without a `claude`
 * binary.
 *
 * The file belongs to the user and it can hold the servers of other products, so every case below
 * asks one question: what survives the merge? The cases run on a temporary directory and they reach
 * no store of the user.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpJsonStoreTest {
    @TempDir
    lateinit var solution: Path

    private val unity = Path.of("/home/tester/.unity/bin/unity")
    private val junieEntry = UnityMcpServerEntry("/home/tester/.unity/bin/unity", listOf("mcp"))

    @Test
    fun `the Codex store is the only one a tool still writes`() {
        assertNull(unityMcpOwnedStore(UnityMcpStore.CODEX_CLI, unity, solution), "the Unity CLI writes the Codex store")
        val owned = UnityMcpStore.entries.filter { unityMcpOwnedStore(it, unity, solution) != null }
        assertEquals(
            listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI, UnityMcpStore.JUNIE_CLI),
            owned,
        )
    }

    /**
     * Two rows read `<solution>/.mcp.json`, and they must write the same bytes into it.
     *
     * Each one puts the same server name in the same file, so a configure on one row would rewrite
     * what the other row put there. A difference here would make the two rows fight.
     */
    @Test
    fun `stores that share a file build the same entry`() {
        for (store in UnityMcpStore.entries) {
            val sharers = unityMcpStoreSharers(store)
            val entry = unityMcpOwnedStore(store, unity, solution)?.entry
            for (sharer in sharers) {
                assertEquals(
                    entry,
                    unityMcpOwnedStore(sharer, unity, solution)?.entry,
                    "the $store row and the $sharer row disagree on the entry of one file",
                )
            }
        }
    }

    /**
     * A batch that already names every row it changes asks the user nothing.
     *
     * "Configure all" is that case, and a single click on a shared row is the other one.
     */
    @Test
    fun `a batch names the rows it also unconfigures`() {
        assertEquals(
            listOf(UnityMcpStore.COPILOT_CLI),
            unityMcpUnnamedSharers(listOf(UnityMcpStore.CLAUDE_CODE_CLI)),
        )
        assertEquals(
            emptyList<UnityMcpStore>(),
            unityMcpUnnamedSharers(listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI)),
            "the batch holds both rows, so the user already asked for both",
        )
        assertEquals(
            emptyList<UnityMcpStore>(),
            unityMcpUnnamedSharers(UnityMcpStore.entries),
            "Remove All reaches every row, so it surprises nobody",
        )
        assertEquals(emptyList<UnityMcpStore>(), unityMcpUnnamedSharers(listOf(UnityMcpStore.JUNIE_CLI)))
    }

    /**
     * A store file must be written one time for each batch.
     *
     * Every writer of a file uses one temp file name, so a second writer of the same file would
     * race the first and leave the store of the user torn.
     */
    @Test
    fun `no batch writes one store file twice`() {
        val batches = listOf(
            listOf(UnityMcpStore.CLAUDE_CODE_CLI),
            listOf(UnityMcpStore.COPILOT_CLI),
            listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI),
            UnityMcpStore.entries,
        )
        for (batch in batches) {
            val written = unityMcpWriteGroups(batch).mapNotNull { unityMcpJsonStoreFile(it.first(), solution) }
            assertEquals(written.distinct(), written, "the batch $batch writes one file twice")
        }
    }

    /** A click on one shared row still writes the file of the row the user did not click. */
    @Test
    fun `a group holds every row over the file it writes`() {
        assertEquals(
            listOf(listOf(UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI)),
            unityMcpWriteGroups(listOf(UnityMcpStore.CLAUDE_CODE_CLI)),
        )
        assertEquals(
            listOf(listOf(UnityMcpStore.COPILOT_CLI, UnityMcpStore.CLAUDE_CODE_CLI)),
            unityMcpWriteGroups(listOf(UnityMcpStore.COPILOT_CLI)),
            "the group starts at the row the user clicked",
        )
        assertEquals(
            listOf(listOf(UnityMcpStore.JUNIE_CLI)),
            unityMcpWriteGroups(listOf(UnityMcpStore.JUNIE_CLI)),
        )
    }

    /** Every row of a batch lands in exactly one group, so every row gets exactly one answer. */
    @Test
    fun `every row of a batch reaches one group`() {
        val groups = unityMcpWriteGroups(UnityMcpStore.entries)
        val rows = groups.flatten()

        assertEquals(UnityMcpStore.entries.toSet(), rows.toSet())
        assertEquals(rows.distinct(), rows, "a row landed in two groups and would get two answers")
    }

    /**
     * The GitHub Copilot CLI was never installed on the measuring machine, so its binary name is unmeasured.
     *
     * So its row does not look for a binary, and it must not report the agent as missing. No write
     * needs the answer either, because Rider writes that store.
     */
    @Test
    fun `each row probes the binary of its agent, and Copilot probes none`() {
        assertEquals(
            mapOf(
                UnityMcpStore.CODEX_CLI to "codex",
                UnityMcpStore.CLAUDE_CODE_CLI to "claude",
                UnityMcpStore.JUNIE_CLI to "junie",
                UnityMcpStore.COPILOT_CLI to null,
            ),
            UnityMcpStore.entries.associateWith { unityMcpAgentBinary(it) },
        )
    }

    /** An agent off the PATH is still found in the folders that the Terminal plugin probes. */
    @Test
    fun `each agent has the well-known folders of the Terminal plugin`() {
        val home = Path.of("/home/tester")
        val localBin = home.resolve(".local/bin")
        val npm = home.resolve("AppData/Roaming/npm")
        val usrLocalBin = Path.of("/usr/local/bin")
        val cases = listOf(
            Triple("codex", OS.macOS, listOf(localBin.resolve("codex"), usrLocalBin.resolve("codex"))),
            Triple("codex", OS.Windows, listOf("codex.exe", "codex.bat", "codex.cmd").map { npm.resolve(it) }),
            Triple("claude", OS.Linux, listOf(localBin.resolve("claude"), usrLocalBin.resolve("claude"))),
            Triple(
                "claude",
                OS.Windows,
                listOf(npm, localBin).flatMap { folder -> listOf("claude.exe", "claude.bat", "claude.cmd").map { folder.resolve(it) } },
            ),
            Triple("junie", OS.macOS, listOf(localBin.resolve("junie"))),
            Triple("junie", OS.Windows, listOf(localBin.resolve("junie.bat"))),
            Triple("copilot", OS.macOS, emptyList()),
        )
        for ((binary, os, expected) in cases) {
            assertEquals(expected, unityMcpAgentWellKnownCandidates(binary, os, home), "$binary on $os")
        }
    }

    @Test
    fun `a configure moves the Rider entry after every other server`() {
        val merged = unityMcpMergedStore(CLAUDE_WRITTEN_STORE, junieEntry)

        assertEquals(
            listOf("idea", "unity-editor-mcp"),
            JsonParser.parseString(merged).asJsonObject.getAsJsonObject("mcpServers").keySet().toList(),
        )
    }

    /**
     * The entry carries no project pin.
     *
     * The store sits inside the solution, so the pin adds nothing. It would also put an absolute path
     * of this machine into a file under the version control of the user.
     */
    @Test
    fun `the Junie entry names the Unity CLI absolutely and carries the bare subcommand`() {
        val owned = unityMcpOwnedStore(UnityMcpStore.JUNIE_CLI, unity, solution)

        assertEquals(solution.resolve(".junie").resolve("mcp").resolve("mcp.json"), owned?.file)
        assertEquals(unity.toString(), owned?.entry?.command)
        assertEquals(listOf("mcp"), owned?.entry?.args)
        assertNull(owned?.entry?.type, "the Junie store holds no type field, measured on 2026-09-30")
    }

    @Test
    fun `the Codex store is no JSON file, so it has no path here`() {
        assertNull(unityMcpJsonStoreFile(UnityMcpStore.CODEX_CLI, solution), "the Codex store is TOML")
        assertEquals(solution.resolve(".mcp.json"), unityMcpJsonStoreFile(UnityMcpStore.CLAUDE_CODE_CLI, solution))
        assertEquals(
            solution.resolve(".mcp.json"),
            unityMcpJsonStoreFile(UnityMcpStore.COPILOT_CLI, solution),
            "the two rows must name one file, or they could report different states",
        )
    }

    /**
     * The entry of `<solution>/.mcp.json` keeps the `type` that `claude mcp add --scope project`
     * wrote.
     *
     * A store that an earlier Rider wrote through `claude` must still read as configured, and the
     * GitHub Copilot CLI accepts `"type": "stdio"`, measured on 2026-09-30. The `type` is the one
     * field on which the two entry shapes differ.
     */
    @Test
    fun `the project store entry keeps the stdio type`() {
        val owned = unityMcpOwnedStore(UnityMcpStore.CLAUDE_CODE_CLI, unity, solution)

        assertEquals(solution.resolve(".mcp.json"), owned?.file)
        assertEquals("stdio", owned?.entry?.type)
        assertNull(unityMcpOwnedStore(UnityMcpStore.JUNIE_CLI, unity, solution)?.entry?.type)
    }

    /**
     * No entry that Rider writes carries a project pin.
     *
     * Every store sits inside the solution, so the working directory of the agent already names the
     * Unity project. A pin would put an absolute path of this machine into a file under the version
     * control of the user. The dev ruled this for every store on 2026-09-30, which is scope call
     * 20, and it settles the one disagreement the Copilot build left open.
     */
    @Test
    fun `no entry that Rider writes names a project`() {
        for (store in UnityMcpStore.entries) {
            val entry = unityMcpOwnedStore(store, unity, solution)?.entry ?: continue
            assertEquals(listOf("mcp"), entry.args, "the entry of the $store store names a project")
        }
    }

    /**
     * The entry writes `type` before `command`, as `claude mcp add` did.
     *
     * A diff of the store of a user must show the entry moving and not rewriting. The path is not
     * asserted here, because a JSON string of a Windows path carries its own escaping.
     */
    @Test
    fun `the project store entry writes the type first`() {
        val owned = unityMcpOwnedStore(UnityMcpStore.CLAUDE_CODE_CLI, unity, solution)
        val merged = unityMcpMergedStore(null, owned!!.entry)!!

        assertTrue(merged.indexOf(""""type": "stdio"""") < merged.indexOf(""""command""""), merged)
        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(merged, fileExists = true))
    }

    /**
     * A store that `claude mcp add --scope project` wrote still reads as configured.
     *
     * Scope call 19 changed the writer of this file on green code, so the bytes of the old writer
     * must keep their meaning.
     */
    @Test
    fun `a store written by claude mcp add still reads as configured`() {
        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(CLAUDE_WRITTEN_STORE, fileExists = true))
    }

    /** A removal over the old bytes drops the entry, and the merge of Rider keeps the siblings. */
    @Test
    fun `a removal over a store written by claude mcp add drops the entry`() {
        val merged = unityMcpMergedStore(CLAUDE_WRITTEN_STORE, null)

        assertEquals(UnityMcpEntry.ABSENT, readMcpServersEntry(merged, fileExists = true))
    }

    /** The shape of the file that Rider creates. The two-space indent is the one of the platform writer. */
    @Test
    fun `a store with no file gets the whole file`() {
        assertEquals(
            """
            {
              "mcpServers": {
                "unity-editor-mcp": {
                  "command": "/home/tester/.unity/bin/unity",
                  "args": [
                    "mcp"
                  ]
                }
              }
            }

            """.trimIndent(),
            unityMcpMergedStore(null, junieEntry),
        )
    }

    @Test
    fun `an empty file gets the whole file too`() {
        assertEquals(unityMcpMergedStore(null, junieEntry), unityMcpMergedStore("   \n", junieEntry))
    }

    /** The store of this machine held `idea` and `rider` beside the Unity entry on 2026-09-30. */
    @Test
    fun `every other server survives a configure`() {
        val merged = unityMcpMergedStore(OTHER_SERVERS, junieEntry)

        assertTrue(merged?.contains(""""idea"""") == true, merged)
        assertTrue(merged?.contains(""""rider"""") == true, merged)
        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(merged, fileExists = true))
    }

    /** The platform writer keeps the root-level fields of the file, and so does this one. */
    @Test
    fun `a root-level field survives a configure`() {
        val merged = unityMcpMergedStore(ROOT_FIELD, junieEntry)

        assertTrue(merged?.contains(""""inputs"""") == true, merged)
        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(merged, fileExists = true))
    }

    @Test
    fun `a second configure replaces the entry and adds no second one`() {
        val merged = unityMcpMergedStore(unityMcpMergedStore(null, junieEntry), junieEntry)

        assertEquals(unityMcpMergedStore(null, junieEntry), merged)
    }

    /** Two stores share this writer, and they disagree on one field. */
    @Test
    fun `the writer keeps the type of the entry it is given`() {
        val merged = unityMcpMergedStore(null, UnityMcpServerEntry("/u", listOf("mcp"), type = "stdio"))

        assertTrue(merged?.contains(""""type": "stdio"""") == true, merged)
    }

    @Test
    fun `a removal drops the Unity entry and keeps the others`() {
        val merged = unityMcpMergedStore(unityMcpMergedStore(OTHER_SERVERS, junieEntry), null)

        assertEquals(UnityMcpEntry.ABSENT, readMcpServersEntry(merged, fileExists = true))
        assertTrue(merged?.contains(""""idea"""") == true, "a removal took a server of another product")
    }

    /**
     * Rider writes nothing into a file it does not understand.
     *
     * The user owns this file, and a merge that guesses would drop what it could not read.
     */
    @Test
    fun `a file that Rider cannot read gets no merge`() {
        assertNull(unityMcpMergedStore("not json at all", junieEntry))
        assertNull(unityMcpMergedStore("""["a list, and not an object"]""", junieEntry))
        assertNull(
            unityMcpMergedStore("""{"mcpServers": "a string"}""", junieEntry),
            "an mcpServers key that holds something else is a file Rider cannot merge into",
        )
    }

    @Test
    fun `a configure creates the directories of the store and leaves no temp file`() {
        val file = solution.resolve(".junie").resolve("mcp").resolve("mcp.json")

        assertTrue(writeUnityMcpJsonStore(file, junieEntry))

        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(file.readText(), fileExists = true))
        assertEquals(listOf(file), filesIn(file.parent), "the write left a file beside the store")
    }

    /**
     * The temp file of the write carries a unique name.
     *
     * A fixed `<store>.tmp` would truncate a file of the user with that name, and two writers of
     * one store would share it. The platform writer uses the fixed name, and this one does not.
     */
    @Test
    fun `a write keeps a file of the user that sits at the old temp name`() {
        val file = solution.resolve("mcp.json")
        val backup = solution.resolve("mcp.json.tmp")
        backup.writeText(OTHER_SERVERS)

        assertTrue(writeUnityMcpJsonStore(file, junieEntry))

        assertEquals(OTHER_SERVERS, backup.readText(), "the write destroyed a file of the user")
    }

    @Test
    fun `a removal keeps the file and every other server`() {
        val file = solution.resolve("mcp.json")
        file.writeText(unityMcpMergedStore(OTHER_SERVERS, junieEntry)!!)

        assertTrue(writeUnityMcpJsonStore(file, null))

        assertTrue(file.exists(), "Rider deleted a file of the user")
        assertEquals(UnityMcpEntry.ABSENT, readMcpServersEntry(file.readText(), fileExists = true))
        assertTrue(file.readText().contains(""""idea""""))
    }

    /** Nothing to remove is not a failure, and it must not leave a file either. */
    @Test
    fun `a removal over a missing file writes nothing`() {
        val file = solution.resolve("absent.json")

        assertTrue(writeUnityMcpJsonStore(file, null))

        assertFalse(file.exists(), "a removal created the store it was asked to clean")
    }

    /** A removal that has nothing to remove must not reformat a file of the user. */
    @Test
    fun `a removal over a file without the entry leaves it byte for byte`() {
        val file = solution.resolve("mcp.json")
        file.writeText(OTHER_SERVERS)

        assertTrue(writeUnityMcpJsonStore(file, null))

        assertEquals(OTHER_SERVERS, file.readText())
    }

    @Test
    fun `a removal over a file that does not parse changes nothing and answers false`() {
        val file = solution.resolve("mcp.json")
        file.writeText("not json at all")

        assertFalse(writeUnityMcpJsonStore(file, null))

        assertEquals("not json at all", file.readText())
    }

    @Test
    fun `a removal over a blank file leaves it byte for byte`() {
        val file = solution.resolve("mcp.json")
        file.writeText(" \n")

        assertTrue(writeUnityMcpJsonStore(file, null))

        assertEquals(" \n", file.readText())
    }

    @Test
    fun `a write over a directory at the store path throws and leaves the directory`() {
        val file = Files.createDirectory(solution.resolve("mcp.json"))

        assertThrows(IOException::class.java) { writeUnityMcpJsonStore(file, junieEntry) }

        assertTrue(Files.isDirectory(file))
    }

    @Test
    fun `a configure over a file that does not parse changes nothing and answers false`() {
        val file = solution.resolve("mcp.json")
        file.writeText("not json at all")

        assertFalse(writeUnityMcpJsonStore(file, junieEntry))

        assertEquals("not json at all", file.readText())
        assertEquals(listOf(file), filesIn(solution), "the failed write left a file behind")
    }

    @Test
    fun `a configure over a non-object mcpServers changes nothing and answers false`() {
        val file = solution.resolve("mcp.json")
        file.writeText("""{"mcpServers": ["idea"]}""")

        assertFalse(writeUnityMcpJsonStore(file, junieEntry))

        assertEquals("""{"mcpServers": ["idea"]}""", file.readText())
        assertEquals(listOf(file), filesIn(solution), "the failed write left a file behind")
    }

    /** The store file and nothing else. A temp file left behind would show up here. */
    private fun filesIn(directory: Path): List<Path> =
        Files.list(directory).use { paths -> paths.sorted().toList() }

    private data class OutcomeCase(
        val name: String,
        val written: Boolean,
        val confirmed: Boolean,
        val expected: UnityMcpFailure?,
    )

    /**
     * The store decides here too.
     *
     * The writer answers for the file it wrote, and the re-read answers for the file that the agent
     * will read. So a write that landed and a store that disagrees is still a failure.
     */
    private val outcomeCases = listOf(
        OutcomeCase("the file holds the merge", written = true, confirmed = true, expected = null),
        OutcomeCase("Rider wrote nothing", written = false, confirmed = false, expected = UnityMcpFailure.FILE_WRITE),
        OutcomeCase(
            "the write landed and the re-read disagrees",
            written = true,
            confirmed = false,
            expected = UnityMcpFailure.NOT_CONFIRMED,
        ),
    )

    @Test
    fun `the store decides the outcome of a write that Rider performed`() {
        for (case in outcomeCases) {
            assertEquals(case.expected, unityMcpFileWriteOutcome(case.written, case.confirmed), case.name)
        }
    }

    private companion object {
        /** The two keys that the platform MCP Server page writes into the same file. */
        const val OTHER_SERVERS = """{"mcpServers": {"idea": {"command": "idea"}, "rider": {"command": "rider"}}}"""

        const val ROOT_FIELD = """{"inputs": [{"id": "token"}], "mcpServers": {}}"""

        /**
         * What `claude mcp add --scope project` wrote into `<solution>/.mcp.json` until scope call 19.
         *
         * It carries the project pin that scope call 20 then dropped, so this text also proves that
         * a pinned entry of an older Rider still reads and still removes.
         */
        const val CLAUDE_WRITTEN_STORE = """
            {
              "mcpServers": {
                "unity-editor-mcp": {
                  "type": "stdio",
                  "command": "/home/tester/.unity/bin/unity",
                  "args": ["mcp", "--project-path", "/home/tester/projects/Platformer"],
                  "env": {}
                },
                "idea": {"command": "idea"}
              }
            }
        """
    }
}
