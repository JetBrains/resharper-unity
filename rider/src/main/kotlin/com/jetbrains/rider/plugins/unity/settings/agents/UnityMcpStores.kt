package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.GsonBuilder
import com.google.gson.JsonSyntaxException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.util.system.OS
import java.nio.file.Path

const val UNITY_MCP_CODEX_KEY: String = "codex"
const val UNITY_MCP_CODEX_SERVER_NAME: String = "unity"
const val UNITY_MCP_RIDER_SERVER_NAME: String = "unity-editor-mcp"
const val UNITY_MCP_CODEX_BINARY: String = "codex"
const val UNITY_MCP_CLAUDE_CODE_BINARY: String = "claude"
const val UNITY_MCP_JUNIE_BINARY: String = "junie"
const val UNITY_MCP_CODEX_DIRECTORY: String = ".codex"
const val UNITY_MCP_PROJECT_STORE_FILE: String = ".mcp.json"
const val UNITY_MCP_JUNIE_STORE_FILE: String = ".junie/mcp/mcp.json"
private const val UNITY_MCP_STATUS_CONFIGURED = "configured"

private val gson = GsonBuilder().disableHtmlEscaping().create()

fun unityMcpLocalClientListCommandLine(unity: Path, solution: Path?): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "mcp", "configure", "--list", "--local", "--json")
        .withWorkingDirectory(solution)

data class UnityMcpClient(
    val key: String? = null,
    val displayName: String? = null,
    val configPath: String? = null,
    val status: String? = null,
)

data class UnityMcpClientList(val clients: List<UnityMcpClient> = emptyList())

fun parseUnityMcpClientList(stdout: String): UnityMcpClientList? {
    val envelope = parseJson<ClientListEnvelope>(stdout) ?: return null
    return UnityMcpClientList(clients = envelope.data.orEmpty().filterNotNull())
}

fun UnityMcpClientList.clientOf(key: String): UnityMcpClient? = clients.firstOrNull { it.key == key }

fun UnityMcpClientList.configPathOf(key: String): String? = clientOf(key)?.configPath

fun readCodexEntry(list: UnityMcpClientList?): UnityMcpEntry {
    val status = list?.clientOf(UNITY_MCP_CODEX_KEY)?.status ?: return UnityMcpEntry.UNREADABLE
    return if (status == UNITY_MCP_STATUS_CONFIGURED) UnityMcpEntry.PRESENT else UnityMcpEntry.ABSENT
}

fun unityMcpJsonStoreName(store: UnityMcpStore): String? = when (store) {
    UnityMcpStore.CODEX_CLI -> null
    UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI -> UNITY_MCP_PROJECT_STORE_FILE
    UnityMcpStore.JUNIE_CLI -> UNITY_MCP_JUNIE_STORE_FILE
}

fun unityMcpJsonStoreFile(store: UnityMcpStore, solution: Path): Path? =
    unityMcpJsonStoreName(store)?.let { solution.resolve(it) }

fun unityMcpStoreSharers(store: UnityMcpStore): List<UnityMcpStore> {
    val name = unityMcpJsonStoreName(store) ?: return emptyList()
    return UnityMcpStore.entries.filter { it != store && unityMcpJsonStoreName(it) == name }
}

fun unityMcpUnnamedSharers(stores: List<UnityMcpStore>): List<UnityMcpStore> =
    stores.flatMap { unityMcpStoreSharers(it) }.distinct().filterNot { it in stores }

fun unityMcpWriteGroups(stores: List<UnityMcpStore>): List<List<UnityMcpStore>> =
    stores
        .map { store -> listOf(store) + unityMcpStoreSharers(store) }
        .distinctBy { it.toSet() }

fun unityMcpAgentBinary(store: UnityMcpStore): String? = when (store) {
    UnityMcpStore.CODEX_CLI -> UNITY_MCP_CODEX_BINARY
    UnityMcpStore.CLAUDE_CODE_CLI -> UNITY_MCP_CLAUDE_CODE_BINARY
    UnityMcpStore.JUNIE_CLI -> UNITY_MCP_JUNIE_BINARY
    UnityMcpStore.COPILOT_CLI -> null
}

/** The folders of `DefaultTerminalAgentsProvider`, which the Terminal plugin probes after the PATH. */
fun unityMcpAgentWellKnownCandidates(binary: String, os: OS, userHome: Path): List<Path> {
    val localBin = userHome.resolve(".local").resolve("bin")
    val npm = userHome.resolve("AppData").resolve("Roaming").resolve("npm")
    val usrLocalBin = Path.of("/usr/local/bin")
    val folders = when (binary) {
        UNITY_MCP_CODEX_BINARY -> if (os == OS.Windows) listOf(npm) else listOf(localBin, usrLocalBin)
        UNITY_MCP_CLAUDE_CODE_BINARY -> if (os == OS.Windows) listOf(npm, localBin) else listOf(localBin, usrLocalBin)
        UNITY_MCP_JUNIE_BINARY -> listOf(localBin)
        else -> emptyList()
    }
    val names = when {
        os != OS.Windows -> listOf(binary)
        binary == UNITY_MCP_JUNIE_BINARY -> listOf("$binary.bat")
        else -> listOf("$binary.exe", "$binary.bat", "$binary.cmd")
    }
    return folders.flatMap { folder -> names.map { folder.resolve(it) } }
}

fun unityMcpRowSnippet(store: UnityMcpStore, unity: Path): String =
    unityMcpStoreEntry(store, unity)?.let(::unityMcpJsonSnippet) ?: unityMcpCodexSnippet(unity)

/** The four tables that `unity mcp configure codex` writes. */
fun unityMcpCodexSnippet(unity: Path): String = """
    [mcp_servers.$UNITY_MCP_CODEX_SERVER_NAME]
    command = ${gson.toJson(unity.toString())}
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
    """.trimIndent()

private inline fun <reified T> parseJson(text: String): T? =
    try {
        gson.fromJson(text, T::class.java)
    }
    catch (_: JsonSyntaxException) {
        null
    }

private data class ClientListEnvelope(val data: List<UnityMcpClient?>? = null)
