package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.GsonBuilder
import com.google.gson.JsonArray
import com.google.gson.JsonNull
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import com.google.gson.JsonSyntaxException
import com.intellij.openapi.application.edtWriteAction
import com.intellij.openapi.application.readAction
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFileManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.nio.file.AccessDeniedException
import java.nio.file.AtomicMoveNotSupportedException
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.StandardCopyOption
import kotlin.io.path.readText

private const val MCP_SERVERS_KEY = "mcpServers"

private const val UNITY_MCP_SUBCOMMAND = "mcp"

private const val UNITY_MCP_STDIO_TYPE = "stdio"

private const val UNITY_MCP_PROJECT_PATH_OPTION = "--project-path"

private val storeGson = GsonBuilder().setPrettyPrinting().disableHtmlEscaping().create()

data class UnityMcpServerEntry(
    val command: String,
    val args: List<String>,
    val type: String? = null,
)

data class UnityMcpOwnedStore(val file: Path, val entry: UnityMcpServerEntry)

fun unityMcpStoreEntry(store: UnityMcpStore, unity: Path): UnityMcpServerEntry? = when (store) {
    UnityMcpStore.CODEX_CLI -> null
    UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI ->
        UnityMcpServerEntry(unity.toString(), listOf(UNITY_MCP_SUBCOMMAND), type = UNITY_MCP_STDIO_TYPE)

    UnityMcpStore.JUNIE_CLI -> UnityMcpServerEntry(unity.toString(), listOf(UNITY_MCP_SUBCOMMAND))
}

fun unityMcpOwnedStore(store: UnityMcpStore, unity: Path, solution: Path): UnityMcpOwnedStore? {
    val entry = unityMcpStoreEntry(store, unity) ?: return null
    val file = unityMcpJsonStoreFile(store, solution) ?: return null
    return UnityMcpOwnedStore(file, entry)
}

fun unityMcpJsonSnippet(entry: UnityMcpServerEntry): String =
    storeGson.toJson(JsonObject().apply { add(UNITY_MCP_RIDER_SERVER_NAME, entry.toJson()) })

fun unityMcpManualSnippet(unity: Path, solution: Path?): String {
    val pin = solution?.let { listOf(UNITY_MCP_PROJECT_PATH_OPTION, it.toString()) }.orEmpty()
    return unityMcpJsonSnippet(UnityMcpServerEntry(unity.toString(), listOf(UNITY_MCP_SUBCOMMAND) + pin))
}

fun readMcpServersEntry(content: String?, fileExists: Boolean): UnityMcpEntry {
    if (!fileExists) return UnityMcpEntry.ABSENT
    if (content == null) return UnityMcpEntry.UNREADABLE
    if (content.isBlank()) return UnityMcpEntry.ABSENT
    val servers = parseObject(content)?.mcpServers() ?: return UnityMcpEntry.UNREADABLE
    return if (servers.has(UNITY_MCP_RIDER_SERVER_NAME)) UnityMcpEntry.PRESENT else UnityMcpEntry.ABSENT
}

fun unityMcpMergedStore(content: String?, entry: UnityMcpServerEntry?): String? {
    val root = if (content.isNullOrBlank()) JsonObject() else parseObject(content) ?: return null
    val servers = root.mcpServers() ?: return null
    servers.remove(UNITY_MCP_RIDER_SERVER_NAME)
    if (entry != null) servers.add(UNITY_MCP_RIDER_SERVER_NAME, entry.toJson())

    root.add(MCP_SERVERS_KEY, servers)
    return storeGson.toJson(root) + "\n"
}

suspend fun writeUnityMcpJsonStoreInIde(file: Path, entry: UnityMcpServerEntry?): Boolean =
    writeStoreInIde(file) { writeUnityMcpJsonStore(file, entry) }

internal suspend fun writeStoreInIde(file: Path, write: () -> Boolean): Boolean {
    return saveUnsavedDocument(file) && withContext(Dispatchers.IO) {
        val written = write()
        if (written) VfsUtil.markDirtyAndRefresh(false, false, false, file)
        written
    }
}

private suspend fun saveUnsavedDocument(file: Path): Boolean {
    val virtualFile = VirtualFileManager.getInstance().findFileByNioPath(file) ?: return true
    val manager = FileDocumentManager.getInstance()
    val document = readAction { manager.getCachedDocument(virtualFile)?.takeIf { manager.isDocumentUnsaved(it) } } ?: return true
    return edtWriteAction {
        manager.saveDocument(document)
        !manager.isDocumentUnsaved(document)
    }
}

fun writeUnityMcpJsonStore(file: Path, entry: UnityMcpServerEntry?): Boolean {
    val content = if (Files.notExists(file)) null else file.readText()
    if (entry == null && readMcpServersEntry(content, fileExists = content != null) == UnityMcpEntry.ABSENT) return true

    val merged = unityMcpMergedStore(content, entry) ?: return false
    replaceStoreFile(file, merged)
    return true
}

internal fun replaceStoreFile(file: Path, text: String) {
    val directory = file.toAbsolutePath().parent
    Files.createDirectories(directory)
    val temp = Files.createTempFile(directory, file.fileName.toString(), ".tmp")
    try {
        Files.writeString(temp, text)
        try {
            Files.move(temp, file, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE)
        }
        catch (_: AtomicMoveNotSupportedException) {
            Files.move(temp, file, StandardCopyOption.REPLACE_EXISTING)
        }
        catch (_: AccessDeniedException) {
            // On Windows an agent can hold the store open, and no move then replaces it.
            Files.writeString(file, text)
        }
    }
    finally {
        Files.deleteIfExists(temp)
    }
}

private fun UnityMcpServerEntry.toJson(): JsonObject = JsonObject().apply {
    type?.let { addProperty("type", it) }
    addProperty("command", command)
    add("args", JsonArray().apply { args.forEach { add(it) } })
}

// Rider owns one key in `mcpServers` and nothing else. Rider cannot merge into a `mcpServers` that is
// not an object. So the read reports it as unreadable, and the write leaves the file alone.
private fun JsonObject.mcpServers(): JsonObject? = when (val servers = get(MCP_SERVERS_KEY)) {
    null, is JsonNull -> JsonObject()
    is JsonObject -> servers
    else -> null
}

private fun parseObject(content: String): JsonObject? =
    try {
        JsonParser.parseString(content) as? JsonObject
    }
    catch (_: JsonSyntaxException) {
        null
    }
