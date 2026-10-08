package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.removeUnityMcpCodexEntry
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
    fun `the removal leaves the sandbox tables, as codex mcp remove does`() {
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
    fun `a missing store is already without the entry, and no file appears`() {
        val file = unityMcpCodexStoreFile(directory)

        assertTrue(removeUnityMcpCodexEntry(file))
        assertFalse(Files.exists(file), "the removal created a store")
    }

    @Test
    fun `the removal writes the store in the solution`() {
        val file = unityMcpCodexStoreFile(directory)
        assertEquals(directory.resolve(".codex").resolve("config.toml"), file)
        Files.createDirectories(file.parent)
        Files.writeString(file, UNITY_CLI_STORE)

        assertTrue(removeUnityMcpCodexEntry(file))
        assertEquals(CODEX_REMOVE_RESULT, file.readText())
    }

    @Test
    fun `a store Rider cannot edit stays as it was`() {
        val file = unityMcpCodexStoreFile(directory)
        Files.createDirectories(file.parent)
        val store = "mcp_servers.unity.command = \"unity\"\n"
        Files.writeString(file, store)

        assertFalse(removeUnityMcpCodexEntry(file))
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
    }
}
