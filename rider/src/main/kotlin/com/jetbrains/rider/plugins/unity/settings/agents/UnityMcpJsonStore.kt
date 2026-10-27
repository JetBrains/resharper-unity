package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.GsonBuilder
import com.google.gson.JsonArray
import com.google.gson.JsonElement
import com.google.gson.JsonNull
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import com.google.gson.JsonPrimitive
import com.google.gson.JsonSyntaxException
import com.intellij.openapi.application.edtWriteAction
import com.intellij.openapi.application.readAction
import com.intellij.openapi.diagnostic.fileLogger
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.ManagingFS
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.ApiStatus
import java.io.IOException
import java.nio.file.AccessDeniedException
import java.nio.file.AtomicMoveNotSupportedException
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.StandardCopyOption
import java.nio.file.attribute.PosixFilePermissions
import kotlin.io.path.readText

private const val MCP_SERVERS_KEY = "mcpServers"

private const val UNITY_MCP_SUBCOMMAND = "mcp"

private const val UNITY_MCP_STDIO_TYPE = "stdio"

private const val UNITY_MCP_PROJECT_PATH_OPTION = "--project-path"

private val storeGson = GsonBuilder().setPrettyPrinting().disableHtmlEscaping().create()

private val logger = fileLogger()

@ApiStatus.Internal
data class UnityMcpServerEntry(
    val command: String,
    val args: List<String>,
    val type: String? = null,
)

@ApiStatus.Internal
data class UnityMcpOwnedStore(val file: Path, val entry: UnityMcpServerEntry)

internal fun unityMcpStoreEntry(store: UnityMcpStore, unity: Path): UnityMcpServerEntry? = when (store) {
    UnityMcpStore.CODEX_CLI -> null
    UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI ->
        UnityMcpServerEntry(unity.toString(), listOf(UNITY_MCP_SUBCOMMAND), type = UNITY_MCP_STDIO_TYPE)

    UnityMcpStore.JUNIE_CLI -> UnityMcpServerEntry(unity.toString(), listOf(UNITY_MCP_SUBCOMMAND))
}

@ApiStatus.Internal
fun unityMcpOwnedStore(store: UnityMcpStore, unity: Path, solution: Path): UnityMcpOwnedStore? {
    val entry = unityMcpStoreEntry(store, unity) ?: return null
    val file = unityMcpJsonStoreFile(store, solution) ?: return null
    return UnityMcpOwnedStore(file, entry)
}

internal fun unityMcpJsonSnippet(entry: UnityMcpServerEntry): String =
    storeGson.toJson(JsonObject().apply { add(UNITY_MCP_SERVER_NAME, entry.toJson()) })

@ApiStatus.Internal
fun unityMcpManualSnippet(unity: Path, solution: Path?): String {
    val pin = solution?.let { listOf(UNITY_MCP_PROJECT_PATH_OPTION, it.toString()) }.orEmpty()
    return unityMcpJsonSnippet(UnityMcpServerEntry(unity.toString(), listOf(UNITY_MCP_SUBCOMMAND) + pin))
}

@ApiStatus.Internal
fun readMcpServersEntry(content: String?, fileExists: Boolean, unity: Path?): UnityMcpEntry {
    if (!fileExists) return UnityMcpEntry.ABSENT
    if (content == null) return UnityMcpEntry.UNREADABLE
    if (content.isBlank()) return UnityMcpEntry.ABSENT
    val servers = parseObject(content)?.mcpServers()?.entrySet()?.map { it.value } ?: return UnityMcpEntry.UNREADABLE
    return when {
        servers.any { it.isUnityMcpServer(unity) } -> UnityMcpEntry.PRESENT
        servers.any { it.isUnityMcpServer() } -> UnityMcpEntry.OTHER_CLI
        else -> UnityMcpEntry.ABSENT
    }
}

@ApiStatus.Internal
fun unityMcpConfigureReplacesOtherServer(content: String?): Boolean {
    if (content.isNullOrBlank()) return false
    val server = parseObject(content)?.mcpServers()?.get(UNITY_MCP_SERVER_NAME) ?: return false
    return !server.isUnityMcpServer()
}

@ApiStatus.Internal
fun unityMcpMergedStore(content: String?, entry: UnityMcpServerEntry?): String? {
    val root = if (content.isNullOrBlank()) JsonObject() else parseObject(content) ?: return null
    val servers = root.mcpServers() ?: return null
    servers.entrySet().removeIf { it.value.isUnityMcpServer() }
    if (entry != null) {
        servers.remove(UNITY_MCP_SERVER_NAME)
        servers.add(UNITY_MCP_SERVER_NAME, entry.toJson())
    }

    root.add(MCP_SERVERS_KEY, servers)
    return storeGson.toJson(root) + "\n"
}

@ApiStatus.Internal
suspend fun writeUnityMcpJsonStoreInIde(file: Path, entry: UnityMcpServerEntry?): Boolean =
    writeStoreInIde(file) { writeUnityMcpJsonStore(file, entry) }

internal suspend fun writeStoreInIde(file: Path, write: suspend () -> Boolean): Boolean {
    return saveUnsavedDocument(file) && withContext(Dispatchers.IO) {
        flushPendingVfsWrites(file)
        val written = write()
        if (written) VfsUtil.markDirtyAndRefresh(false, false, false, *refreshedByWrite(file))
        written
    }
}

private fun refreshedByWrite(file: Path): Array<Path> =
    if (Files.exists(file)) arrayOf(file) else arrayOf(file, file.toAbsolutePath().parent)

private suspend fun saveUnsavedDocument(file: Path): Boolean {
    val virtualFile = VirtualFileManager.getInstance().findFileByNioPath(file) ?: return true
    val manager = FileDocumentManager.getInstance()
    val document = readAction { manager.getCachedDocument(virtualFile)?.takeIf { manager.isDocumentUnsaved(it) } } ?: return true
    val saved = edtWriteAction {
        manager.saveDocument(document)
        !manager.isDocumentUnsaved(document)
    }
    if (!saved) logger.warn("Rider could not save the open document of $file, so it did not write the file")
    return saved
}

private fun flushPendingVfsWrites(file: Path) {
    val virtualFile = VirtualFileManager.getInstance().findFileByNioPath(file) ?: return
    ManagingFS.getInstance().flushPendingUpdates(virtualFile)
}

@ApiStatus.Internal
fun writeUnityMcpJsonStore(file: Path, entry: UnityMcpServerEntry?): Boolean {
    val content = if (Files.notExists(file)) null else file.readText()
    // The removal drops the entry of any Unity CLI.
    if (entry == null && readMcpServersEntry(content, fileExists = content != null, unity = null) == UnityMcpEntry.ABSENT) return true

    val merged = unityMcpMergedStore(content, entry) ?: return false
    replaceStoreFile(file, merged)
    return true
}

internal fun replaceStoreFile(file: Path, text: String) {
    val directory = file.toAbsolutePath().parent
    Files.createDirectories(directory)

    val isPosix = "posix" in file.fileSystem.supportedFileAttributeViews()
    val attributes = when {
        !isPosix -> emptyArray()
        else -> arrayOf(PosixFilePermissions.asFileAttribute(PosixFilePermissions.fromString("rw-r--r--")))
    }
    val temp = Files.createTempFile(directory, file.fileName.toString(), ".tmp", *attributes)
    try {
        Files.writeString(temp, text)
        if (isPosix) {
            try {
                val permissions = Files.getPosixFilePermissions(file)
                Files.setPosixFilePermissions(temp, permissions)
            }
            catch (_: IOException) {
                // ignore errors
            }
        }
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

private fun JsonElement.isUnityMcpServer(unity: Path? = null): Boolean {
    val server = this as? JsonObject ?: return false
    val command = (server.get("command") as? JsonPrimitive)?.asString ?: return false
    val subcommand = ((server.get("args") as? JsonArray)?.firstOrNull() as? JsonPrimitive)?.asString
    return command.substringAfterLast('/').substringAfterLast('\\').removeSuffix(".exe") == UNITY_CLI_COMMAND &&
           subcommand == UNITY_MCP_SUBCOMMAND &&
           (unity == null || isUnityCliCommand(command, unity))
}

// Rider owns the Unity servers in `mcpServers` and nothing else. Rider cannot merge into a `mcpServers` that is
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
