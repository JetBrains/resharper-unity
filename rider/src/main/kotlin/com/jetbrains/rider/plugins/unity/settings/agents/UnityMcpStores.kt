package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.GsonBuilder
import com.intellij.openapi.util.io.FileUtil
import org.jetbrains.annotations.ApiStatus
import java.nio.file.Path

internal const val UNITY_MCP_SERVER_NAME: String = "unity"
internal const val UNITY_MCP_CODEX_DIRECTORY: String = ".codex"
private const val UNITY_MCP_PROJECT_STORE_FILE: String = ".mcp.json"
private const val UNITY_MCP_JUNIE_STORE_FILE: String = ".junie/mcp/mcp.json"

private val gson = GsonBuilder().disableHtmlEscaping().create()

/**
 * Reads the `[mcp_servers.unity]` table of the Codex store.
 *
 * A table of another MCP server holds no Unity entry. [unityMcpCodexConfigureReplacesOtherServer] finds such a table.
 */
@ApiStatus.Internal
fun readCodexEntry(content: String?, fileExists: Boolean, unity: Path?): UnityMcpEntry {
    if (!fileExists) return UnityMcpEntry.ABSENT
    if (content == null) return UnityMcpEntry.UNREADABLE
    val hasEntry = hasUnityMcpCodexEntry(content) ?: return UnityMcpEntry.UNREADABLE
    if (!hasEntry || unityMcpCodexConfigureReplacesOtherServer(content, unity)) return UnityMcpEntry.ABSENT
    val command = unityMcpCodexCommand(content)
    return if (unity != null && command != null && !isUnityCliCommand(command, unity)) UnityMcpEntry.OTHER_CLI
    else UnityMcpEntry.PRESENT
}

internal fun isUnityCliCommand(command: String, unity: Path): Boolean {
    val bareName = '/' !in command && '\\' !in command
    return if (bareName) command.removeSuffix(".exe") == UNITY_CLI_COMMAND
    else FileUtil.pathsEqual(command, unity.toString())
}

/** Tells whether [command] runs a program named `unity`, from the PATH or from a path. */
internal fun namesUnityCli(command: String): Boolean =
    command.substringAfterLast('/').substringAfterLast('\\').removeSuffix(".exe") == UNITY_CLI_COMMAND

internal fun unityMcpJsonStoreName(store: UnityMcpStore): String? = when (store) {
    UnityMcpStore.CODEX_CLI -> null
    UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI -> UNITY_MCP_PROJECT_STORE_FILE
    UnityMcpStore.JUNIE_CLI -> UNITY_MCP_JUNIE_STORE_FILE
}

@ApiStatus.Internal
fun unityMcpJsonStoreFile(store: UnityMcpStore, solution: Path): Path? =
    unityMcpJsonStoreName(store)?.let { solution.resolve(it) }

@ApiStatus.Internal
fun unityMcpStoreSharers(store: UnityMcpStore): List<UnityMcpStore> {
    val name = unityMcpJsonStoreName(store) ?: return emptyList()
    return UnityMcpStore.entries.filter { it != store && unityMcpJsonStoreName(it) == name }
}

@ApiStatus.Internal
fun unityMcpUnnamedSharers(stores: List<UnityMcpStore>): List<UnityMcpStore> =
    stores.flatMap { unityMcpStoreSharers(it) }.distinct().filterNot { it in stores }

@ApiStatus.Internal
fun unityMcpWriteGroups(stores: List<UnityMcpStore>): List<List<UnityMcpStore>> =
    stores
        .map { store -> listOf(store) + unityMcpStoreSharers(store) }
        .distinctBy { it.toSet() }

@ApiStatus.Internal
fun unityMcpRowSnippet(store: UnityMcpStore, unity: Path): String =
    unityMcpStoreEntry(store, unity)?.let(::unityMcpJsonSnippet) ?: unityMcpCodexSnippet(unity)

/** The `[mcp_servers.unity]` table that runs [unity] as the MCP server. */
internal fun unityMcpCodexEntry(unity: Path): String = """
    [mcp_servers.$UNITY_MCP_SERVER_NAME]
    command = ${gson.toJson(unity.toString())}
    args = ["mcp"]
    """.trimIndent()

/** The four tables that Rider writes into the Codex store. */
@ApiStatus.Internal
fun unityMcpCodexSnippet(unity: Path): String = unityMcpCodexEntry(unity) + "\n\n" + """
    [sandbox_workspace_write]
    network_access = true

    [features.network_proxy]
    enabled = true
    allow_local_binding = false

    [features.network_proxy.domains]
    localhost = "allow"
    "127.0.0.1" = "allow"
    "::1" = "allow"
    """.trimIndent()
