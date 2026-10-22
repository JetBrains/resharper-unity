package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.removeUnityMcpCodexEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCodexRemoval
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCodexSnippet
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCodexStoreFile
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpCodexStoreWithoutEntry
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.condition.EnabledOnOs
import org.junit.jupiter.api.condition.OS
import org.junit.jupiter.api.io.TempDir
import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.readText

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpCodexStoreTest {
    @TempDir
    lateinit var directory: Path

    @Test
    fun `the network policy stays without the entry, as codex mcp remove leaves it`() {
        assertEquals(CODEX_REMOVE_RESULT, unityMcpCodexStoreWithoutEntry(UNITY_CLI_STORE))
    }

    @Test
    fun `another server, its comments and its sub-tables stay as they are`() {
        val store = """
            # my servers
            [mcp_servers.github]
            command = "gh"   # keep this
            args = [
              "mcp",
            ]

            [mcp_servers.unity]
            command = "/home/tester/.unity/bin/unity"
            args = ["mcp"]

            [mcp_servers.unity.env]
            UNITY_QUIET = "1"

            [mcp_servers.github.env]
            TOKEN = "x"
        """.trimIndent()

        val expected = """
            # my servers
            [mcp_servers.github]
            command = "gh"   # keep this
            args = [
              "mcp",
            ]

            [mcp_servers.github.env]
            TOKEN = "x"
        """.trimIndent()
        assertEquals(expected, unityMcpCodexStoreWithoutEntry(store))
    }

    @Test
    fun `Windows line endings stay`() {
        val store = "[mcp_servers.unity]\r\ncommand = \"unity\"\r\n\r\n[sandbox_workspace_write]\r\nnetwork_access = true\r\n"

        assertEquals("[sandbox_workspace_write]\r\nnetwork_access = true\r\n", unityMcpCodexStoreWithoutEntry(store))
    }

    @Test
    fun `a quoted header names the same entry`() {
        val store = "[ mcp_servers . \"unity\" ]\ncommand = \"unity\"\n[other]\nkey = 1\n"

        assertEquals("[other]\nkey = 1\n", unityMcpCodexStoreWithoutEntry(store))
    }

    @Test
    fun `a store with no Unity entry is not changed`() {
        val store = "[mcp_servers.unity-editor]\ncommand = \"x\"\n[mcp_servers.unityx]\ncommand = \"y\"\n"

        assertEquals(store, unityMcpCodexStoreWithoutEntry(store))
    }

    @Test
    fun `a header inside a multi-line string is text and not a table`() {
        val store = "[notes]\ntext = \"\"\"\n[mcp_servers.unity]\n\"\"\"\nafter = 1\n"

        assertEquals(store, unityMcpCodexStoreWithoutEntry(store))
    }

    @Test
    fun `a header inside a multi-line array of the entry goes with the entry`() {
        val store = "[mcp_servers.unity]\nargs = [\n[\"mcp\"]\n]\n[other]\nkey = 1\n"

        assertEquals("[other]\nkey = 1\n", unityMcpCodexStoreWithoutEntry(store))
    }

    @Test
    fun `an entry Rider cannot remove line by line leaves the file alone`() {
        val stores = listOf(
            "mcp_servers.unity.command = \"unity\"\n",
            "[mcp_servers]\nunity = { command = \"unity\" }\n",
            "[mcp_servers]\nunity.command = \"unity\"\n",
            "mcp_servers = { unity = { command = \"unity\" } }\n",
        )
        for (store in stores) {
            assertNull(unityMcpCodexStoreWithoutEntry(store), store)
        }
    }

    @Test
    fun `a file that does not parse leaves the file alone`() {
        val stores = listOf(
            "[mcp_servers.unity\ncommand = \"unity\"\n",
            "[mcp_servers.unity] trailing\n",
            "key\n",
            "[other]\ntext = \"\"\"\nnever closed\n",
        )
        for (store in stores) {
            assertNull(unityMcpCodexStoreWithoutEntry(store), store)
        }
    }

    @Test
    fun `a store with only what the Unity CLI wrote goes whole`() {
        val removal = unityMcpCodexRemoval(UNITY_CLI_STORE)!!

        assertEquals("", removal.withoutUnity)
        assertTrue(removal.deletesStore)
        assertFalse(removal.asksForNetworkPolicy)
    }

    @Test
    fun `a store with only the entry goes whole`() {
        val removal = unityMcpCodexRemoval("[mcp_servers.unity]\ncommand = \"unity\"\nargs = [\"mcp\"]\n")!!

        assertTrue(removal.deletesStore)
    }

    @Test
    fun `the Copy snippet of the Codex row is what a removal takes whole`() {
        assertTrue(unityMcpCodexRemoval(unityMcpCodexSnippet(Path.of("unity")))!!.deletesStore)
    }

    @Test
    fun `the merge of the Unity CLI into a user store asks, and the user content stays`() {
        val removal = unityMcpCodexRemoval(MERGED_STORE)!!

        assertEquals(MERGED_WITHOUT_ENTRY, removal.withoutEntry)
        assertEquals(MERGED_WITHOUT_UNITY, removal.withoutUnity)
        assertTrue(removal.asksForNetworkPolicy)
        assertFalse(removal.deletesStore)
    }

    @Test
    fun `a key or a value that the Unity CLI does not write stays`() {
        val store = """
            [mcp_servers.unity]
            command = "unity"

            [sandbox_workspace_write]
            network_access = false

            [features.network_proxy]
            enabled = true
            mode = "limited"

            [features.network_proxy.domains]
            localhost = "allow"
            "example.com" = "deny"
        """.trimIndent() + "\n"

        val expected = """
            [sandbox_workspace_write]
            network_access = false

            [features.network_proxy]
            mode = "limited"

            [features.network_proxy.domains]
            "example.com" = "deny"
        """.trimIndent() + "\n"
        assertEquals(expected, unityMcpCodexRemoval(store)!!.withoutUnity)
    }

    @Test
    fun `a comment keeps its line and its table`() {
        val store = """
            [mcp_servers.unity]
            command = "unity"

            [sandbox_workspace_write]
            network_access = true # I need this

            [features.network_proxy]
            # my proxy
            enabled = true
            allow_local_binding = false
        """.trimIndent() + "\n"

        val expected = """
            [sandbox_workspace_write]
            network_access = true # I need this

            [features.network_proxy]
            # my proxy
        """.trimIndent() + "\n"
        val removal = unityMcpCodexRemoval(store)!!
        assertEquals(expected, removal.withoutUnity)
        assertTrue(removal.asksForNetworkPolicy)
    }

    @Test
    fun `a dotted key, an inline table and a header with a comment are user content`() {
        val stores = listOf(
            "[mcp_servers.unity]\ncommand = \"unity\"\n[features]\nnetwork_proxy.enabled = true\n",
            "[mcp_servers.unity]\ncommand = \"unity\"\n[features.network_proxy]\ndomains = { localhost = \"allow\" }\n",
            "[mcp_servers.unity]\ncommand = \"unity\"\n[sandbox_workspace_write] # mine\nnetwork_access = true\n",
        )
        for (store in stores) {
            val removal = unityMcpCodexRemoval(store)!!
            assertEquals(removal.withoutEntry, removal.withoutUnity, store)
            assertFalse(removal.deletesStore, store)
        }
    }

    @Test
    fun `quoted keys and literal strings count as the network policy`() {
        val store = """
            [mcp_servers.unity]
            command = "unity"

            [ "sandbox_workspace_write" ]
            "network_access" = true

            [features.'network_proxy'.domains]
            'localhost' = 'allow'
            "127.0.0.1" = "allow"
            '::1' = "allow"
        """.trimIndent() + "\n"

        assertTrue(unityMcpCodexRemoval(store)!!.deletesStore)
    }

    @Test
    fun `a Unity-only store with Windows line endings goes whole, and a mixed one keeps them`() {
        val unityOnly = "[mcp_servers.unity]\r\ncommand = \"unity\"\r\n\r\n[sandbox_workspace_write]\r\nnetwork_access = true\r\n"
        val mixed = "[mcp_servers.github]\r\ncommand = \"gh\"\r\n\r\n[sandbox_workspace_write]\r\nnetwork_access = true\r\n"

        assertTrue(unityMcpCodexRemoval(unityOnly)!!.deletesStore)
        assertEquals("[mcp_servers.github]\r\ncommand = \"gh\"\r\n", unityMcpCodexRemoval(mixed)!!.withoutUnity)
    }

    @Test
    fun `a missing store is already without the entry, and no file appears`() {
        val file = unityMcpCodexStoreFile(directory)

        assertTrue(removeUnityMcpCodexEntry(file, removeNetworkPolicy = false))
        assertFalse(Files.exists(file), "the removal created a store")
    }

    @Test
    fun `the removal deletes a Unity-only store in the solution, and its empty folder`() {
        val file = unityMcpCodexStoreFile(directory)
        assertEquals(directory.resolve(".codex").resolve("config.toml"), file)
        Files.createDirectories(file.parent)
        Files.writeString(file, UNITY_CLI_STORE)

        assertTrue(removeUnityMcpCodexEntry(file, removeNetworkPolicy = false))
        assertFalse(Files.exists(file), "the store stayed")
        assertFalse(Files.exists(file.parent), "the empty folder stayed")
    }

    @Test
    fun `the removal keeps the Codex folder when it holds another file`() {
        val file = unityMcpCodexStoreFile(directory)
        Files.createDirectories(file.parent)
        Files.writeString(file, UNITY_CLI_STORE)
        val other = file.resolveSibling("notes.md")
        Files.writeString(other, "notes")

        assertTrue(removeUnityMcpCodexEntry(file, removeNetworkPolicy = false))
        assertFalse(Files.exists(file), "the store stayed")
        assertTrue(Files.exists(other), "the removal deleted another file")
    }

    @Test
    @EnabledOnOs(OS.WINDOWS)
    fun `the removal succeeds when another process holds the empty Codex folder`() {
        val file = unityMcpCodexStoreFile(directory)
        Files.createDirectories(file.parent)
        Files.writeString(file, UNITY_CLI_STORE)
        // A process that runs in the folder keeps Windows from deleting it, as a terminal there does.
        val holder = ProcessBuilder("ping", "-n", "30", "127.0.0.1")
            .directory(file.parent.toFile())
            .redirectOutput(ProcessBuilder.Redirect.DISCARD)
            .start()
        try {
            assertTrue(removeUnityMcpCodexEntry(file, removeNetworkPolicy = false))
            assertFalse(Files.exists(file), "the store stayed")
        }
        finally {
            holder.destroyForcibly().waitFor()
        }
    }

    @Test
    fun `a mixed store keeps the network policy unless the user agreed to remove it`() {
        val file = unityMcpCodexStoreFile(directory)
        Files.createDirectories(file.parent)

        Files.writeString(file, MERGED_STORE)
        assertTrue(removeUnityMcpCodexEntry(file, removeNetworkPolicy = false))
        assertEquals(MERGED_WITHOUT_ENTRY, file.readText())

        Files.writeString(file, MERGED_STORE)
        assertTrue(removeUnityMcpCodexEntry(file, removeNetworkPolicy = true))
        assertEquals(MERGED_WITHOUT_UNITY, file.readText())
    }

    @Test
    fun `a store Rider cannot edit stays as it was`() {
        val file = unityMcpCodexStoreFile(directory)
        Files.createDirectories(file.parent)
        val store = "mcp_servers.unity.command = \"unity\"\n"
        Files.writeString(file, store)

        assertFalse(removeUnityMcpCodexEntry(file, removeNetworkPolicy = true))
        assertEquals(store, file.readText())
    }

    private companion object {
        /** What `unity mcp configure codex --local --yes` wrote with the Unity CLI beta.12, measured on 2026-10-07. */
        val UNITY_CLI_STORE = "\n" + """
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
        """.trimIndent() + "\n"

        /** What `codex mcp remove unity` left of [UNITY_CLI_STORE], measured on 2026-10-07. */
        val CODEX_REMOVE_RESULT = "\n" + """
            [sandbox_workspace_write]
            network_access = true

            [features.network_proxy]
            enabled = true
            allow_local_binding = false

            [features.network_proxy.domains]
            localhost = "allow"
            "127.0.0.1" = "allow"
            "::1" = "allow"
        """.trimIndent() + "\n"

        /**
         * What `unity mcp configure codex --local --yes` wrote over a user store with the Unity CLI beta.13, measured on
         * 2026-10-22. The user store had `network_access = false` and the host `github.com`.
         */
        val MERGED_STORE = """
            [mcp_servers.github]
            command = "gh"

            [sandbox_workspace_write]
            network_access = true

            [features.network_proxy.domains]
            "github.com" = "allow"
            localhost = "allow"
            "127.0.0.1" = "allow"
            "::1" = "allow"
            [mcp_servers.unity]
            command = "/home/tester/.unity/bin/unity"
            args = ["mcp"]
        """.trimIndent() + "\n"

        val MERGED_WITHOUT_ENTRY = """
            [mcp_servers.github]
            command = "gh"

            [sandbox_workspace_write]
            network_access = true

            [features.network_proxy.domains]
            "github.com" = "allow"
            localhost = "allow"
            "127.0.0.1" = "allow"
            "::1" = "allow"
        """.trimIndent() + "\n"

        val MERGED_WITHOUT_UNITY = """
            [mcp_servers.github]
            command = "gh"

            [features.network_proxy.domains]
            "github.com" = "allow"
        """.trimIndent() + "\n"
    }
}
