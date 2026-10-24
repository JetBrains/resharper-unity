package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil
import org.jetbrains.annotations.ApiStatus
import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.readText

private const val CODEX_STORE_FILE = "config.toml"
private const val MCP_SERVERS_TABLE = "mcp_servers"
private val CODEX_ENTRY = listOf(MCP_SERVERS_TABLE, UNITY_MCP_SERVER_NAME)
private val CODEX_COMMAND = CODEX_ENTRY + "command"

private class NetworkPolicyKey(val table: List<String>, val key: String, val values: Set<String>)

private val SANDBOX_TABLE = listOf("sandbox_workspace_write")
private val NETWORK_PROXY_TABLE = listOf("features", "network_proxy")
private val NETWORK_PROXY_DOMAINS_TABLE = NETWORK_PROXY_TABLE + "domains"
private val DOMAIN_ALLOWED = setOf("\"allow\"", "'allow'")

private val UNITY_NETWORK_POLICY = listOf(
    NetworkPolicyKey(SANDBOX_TABLE, "network_access", setOf("true")),
    NetworkPolicyKey(NETWORK_PROXY_TABLE, "enabled", setOf("true")),
    NetworkPolicyKey(NETWORK_PROXY_TABLE, "allow_local_binding", setOf("false")),
    NetworkPolicyKey(NETWORK_PROXY_DOMAINS_TABLE, "localhost", DOMAIN_ALLOWED),
    NetworkPolicyKey(NETWORK_PROXY_DOMAINS_TABLE, "127.0.0.1", DOMAIN_ALLOWED),
    NetworkPolicyKey(NETWORK_PROXY_DOMAINS_TABLE, "::1", DOMAIN_ALLOWED),
)
private val UNITY_NETWORK_POLICY_TABLES = UNITY_NETWORK_POLICY.map { it.table }.toSet()

@ApiStatus.Internal
fun unityMcpCodexStoreFile(solution: Path): Path =
    solution.resolve(UNITY_MCP_CODEX_DIRECTORY).resolve(CODEX_STORE_FILE)

internal suspend fun removeUnityMcpCodexEntryInIde(file: Path, removeNetworkPolicy: Boolean): Boolean =
    writeStoreInIde(file) { removeUnityMcpCodexEntry(file, removeNetworkPolicy) }

@ApiStatus.Internal
fun removeUnityMcpCodexEntry(file: Path, removeNetworkPolicy: Boolean): Boolean {
    if (Files.notExists(file)) return true
    val content = file.readText()
    val removal = unityMcpCodexRemoval(content) ?: return false
    if (removal.deletesStore) {
        deleteWithEmptyParent(file)
        return true
    }
    val stripped = if (removeNetworkPolicy) removal.withoutUnity else removal.withoutEntry
    if (stripped != content) replaceStoreFile(file, stripped)
    return true
}

@ApiStatus.Internal
data class UnityMcpCodexRemoval(val withoutEntry: String, val withoutUnity: String) {
    val deletesStore: Boolean get() = withoutUnity.isBlank()
    val asksForNetworkPolicy: Boolean get() = !deletesStore && withoutUnity != withoutEntry
}

@ApiStatus.Internal
fun unityMcpCodexRemoval(content: String): UnityMcpCodexRemoval? {
    val withoutEntry = unityMcpCodexStoreWithoutEntry(content) ?: return null
    val withoutUnity = unityMcpCodexStoreWithoutNetworkPolicy(withoutEntry) ?: return null
    return UnityMcpCodexRemoval(withoutEntry, withoutUnity)
}

/**
 * Drops the `[mcp_servers.unity]` table and its sub-tables, and keeps every other line as it is.
 *
 * Codex's sandbox network policy, which `unity mcp configure codex` also writes, stays, as `codex mcp remove`
 * leaves it too. [unityMcpCodexStoreWithoutNetworkPolicy] drops it.
 *
 * Returns null for a file that this line scan cannot edit safely: an entry in a dotted key or an
 * inline table, or a header or a value that does not parse.
 */
@ApiStatus.Internal
fun unityMcpCodexStoreWithoutEntry(content: String): String? {
    val kept = StringBuilder()
    var inEntry = false
    val parsed = walkTomlLines(content) { line, kind ->
        if (kind is TomlLine.Header) inEntry = kind.table.startsWith(CODEX_ENTRY)
        if (kind is TomlLine.KeyValue && !inEntry && (kind.path.startsWith(CODEX_ENTRY) || kind.path == listOf(MCP_SERVERS_TABLE))) {
            return null
        }
        if (!inEntry) kept.append(line)
    }
    return if (parsed) kept.toString() else null
}

/**
 * Reads the `command` of the `[mcp_servers.unity]` table.
 *
 * Returns null when the store has no command, when the command is not a one-line string, or when a line before it
 * does not parse. [readCodexEntry] then reads a configured entry as present.
 */
@ApiStatus.Internal
fun unityMcpCodexCommand(content: String): String? {
    walkTomlLines(content) { _, kind ->
        if (kind is TomlLine.KeyValue && kind.path == CODEX_COMMAND) return readTomlString(kind.value)
    }
    return null
}

/**
 * Drops the keys of Codex's sandbox network policy that `unity mcp configure codex` writes, and keeps every other
 * line as it is.
 *
 * A key goes only under a plain table header, with the exact value of the Unity CLI and with no comment. A table
 * goes whole when it loses its last line, with the blank lines above its header.
 *
 * Returns null for a header or a value that does not parse.
 */
@ApiStatus.Internal
fun unityMcpCodexStoreWithoutNetworkPolicy(content: String): String? {
    val kept = StringBuilder()
    var section = TomlSection(policyTable = null, leading = "", header = null)
    val parsed = walkTomlLines(content) { line, kind ->
        when (kind) {
            TomlLine.Blank -> section.blank(line)
            TomlLine.Comment, TomlLine.Continuation -> section.keep(line)
            is TomlLine.Header -> {
                val policyTable = kind.table.takeIf { it in UNITY_NETWORK_POLICY_TABLES && kind.isPlain }
                val leading = section.takeBlanks()
                section.appendTo(kept)
                section = TomlSection(policyTable, leading, line)
            }
            is TomlLine.KeyValue -> if (section.isUnityPolicyKey(kind.key, kind.value)) section.drop() else section.keep(line)
        }
    }
    if (!parsed) return null
    section.appendTo(kept)
    return kept.toString()
}

private class TomlKey(val path: List<String>, val end: Int)

/** What one line of the store holds. */
private sealed interface TomlLine {
    /** A line inside a value that spans lines. */
    data object Continuation : TomlLine
    data object Blank : TomlLine
    data object Comment : TomlLine
    class Header(val table: List<String>, val isPlain: Boolean) : TomlLine
    class KeyValue(val table: List<String>, val key: List<String>, val value: String) : TomlLine {
        val path: List<String> get() = table + key
    }
}

/**
 * Hands each line of [content] to [visit], with what the line holds.
 *
 * Returns false for a header or a key that does not parse, or for a value that is still open at the end.
 */
private inline fun walkTomlLines(content: String, visit: (line: String, kind: TomlLine) -> Unit): Boolean {
    val value = TomlValueScan()
    var table = emptyList<String>()
    for (line in content.split(Regex("(?<=\n)"))) {
        val kind = if (value.isOpen) TomlLine.Continuation else readTomlLine(line.trim(), table) ?: return false
        when (kind) {
            TomlLine.Continuation -> value.scan(line)
            is TomlLine.Header -> table = kind.table
            is TomlLine.KeyValue -> value.scan(kind.value)
            TomlLine.Blank, TomlLine.Comment -> {}
        }
        visit(line, kind)
    }
    return !value.isOpen
}

private fun readTomlLine(text: String, table: List<String>): TomlLine? = when {
    text.isEmpty() -> TomlLine.Blank
    text.startsWith("#") -> TomlLine.Comment
    text.startsWith("[") -> readTomlHeader(text)?.let { TomlLine.Header(it, isPlainTomlHeader(text)) }
    else -> {
        val key = readTomlKey(text, 0)
        val rest = key?.let { text.substring(it.end) }
        if (key == null || rest == null || !rest.startsWith("=")) null
        else TomlLine.KeyValue(table, key.path, rest.substring(1).trim())
    }
}

/**
 * One table of the store, with the blank lines above its header.
 *
 * A table of the network policy that loses all its lines goes whole, so that no empty header stays.
 */
private class TomlSection(private val policyTable: List<String>?, leading: String, header: String?) {
    private val text = StringBuilder(leading).append(header.orEmpty())
    private val blanks = StringBuilder()
    private var kept = false
    private var dropped = false

    fun isUnityPolicyKey(path: List<String>, value: String): Boolean =
        policyTable != null &&
        UNITY_NETWORK_POLICY.any { it.table == policyTable && path == listOf(it.key) && value in it.values }

    fun keep(line: String) {
        text.append(blanks).append(line)
        blanks.setLength(0)
        kept = true
    }

    fun blank(line: String) {
        blanks.append(line)
    }

    fun drop() {
        dropped = true
    }

    /** Hands the blank lines at the end of this table to the next header. */
    fun takeBlanks(): String = blanks.toString().also { blanks.setLength(0) }

    fun appendTo(out: StringBuilder) {
        if (dropped && !kept) return
        out.append(text).append(blanks)
    }
}

/** A `[table]` header with no comment after it. An array of tables or a comment marks a table that the user wrote. */
private fun isPlainTomlHeader(text: String): Boolean {
    val key = readTomlKey(text, 1) ?: return false
    return text.substring(key.end).trim() == "]"
}

private fun readTomlHeader(text: String): List<String>? {
    val brackets = if (text.startsWith("[[")) 2 else 1
    val key = readTomlKey(text, brackets) ?: return null
    val close = "]".repeat(brackets)
    if (!text.startsWith(close, key.end)) return null
    val rest = text.substring(key.end + brackets).trim()
    return if (rest.isEmpty() || rest.startsWith("#")) key.path else null
}

private fun readTomlKey(text: String, start: Int): TomlKey? {
    val path = mutableListOf<String>()
    var i = start
    while (true) {
        i = skipBlanks(text, i)
        if (i >= text.length) return null
        val c = text[i]
        val end = when {
            c == '"' || c == '\'' -> closingQuote(text, i, c) ?: return null
            isBareKeyChar(c) -> (i until text.length).firstOrNull { !isBareKeyChar(text[it]) } ?: text.length
            else -> return null
        }
        path += if (c == '"' || c == '\'') text.substring(i + 1, end - 1) else text.substring(i, end)
        i = skipBlanks(text, end)
        if (i < text.length && text[i] == '.') i++ else return TomlKey(path, i)
    }
}

/** Follows a value across lines, so that a line inside a multi-line string or array never reads as a header. */
private class TomlValueScan {
    private var depth = 0
    private var multiline: String? = null

    val isOpen: Boolean get() = depth > 0 || multiline != null

    fun scan(text: String) {
        var i = 0
        while (i < text.length) {
            val delimiter = multiline
            if (delimiter != null) {
                val end = text.indexOf(delimiter, i)
                if (end < 0) return
                multiline = null
                i = end + delimiter.length
                continue
            }
            when (val c = text[i]) {
                '#' -> return
                '"', '\'' -> {
                    val triple = "$c$c$c"
                    if (text.startsWith(triple, i)) {
                        multiline = triple
                        i += triple.length
                    }
                    else {
                        i = closingQuote(text, i, c) ?: return
                    }
                    continue
                }
                '[', '{' -> depth++
                ']', '}' -> depth--
            }
            i++
        }
    }
}

private fun readTomlString(text: String): String? {
    val quote = text.firstOrNull()?.takeIf { it == '"' || it == '\'' } ?: return null
    if (text.startsWith("$quote$quote$quote")) return null
    val end = closingQuote(text, 0, quote) ?: return null
    val rest = text.substring(end).trim()
    if (rest.isNotEmpty() && !rest.startsWith("#")) return null
    val body = text.substring(1, end - 1)
    return if (quote == '"') StringUtil.unescapeStringCharacters(body) else body
}

/** The index after the quote that closes the string opened at [start]. A literal string has no escapes. */
private fun closingQuote(text: String, start: Int, quote: Char): Int? {
    var i = start + 1
    while (i < text.length) {
        when (text[i]) {
            '\\' -> if (quote == '"') i++
            quote -> return i + 1
        }
        i++
    }
    return null
}

private fun skipBlanks(text: String, start: Int): Int {
    var i = start
    while (i < text.length && (text[i] == ' ' || text[i] == '\t')) i++
    return i
}

private fun isBareKeyChar(c: Char): Boolean = c.isLetterOrDigit() || c == '_' || c == '-'

private fun List<String>.startsWith(prefix: List<String>): Boolean = size >= prefix.size && subList(0, prefix.size) == prefix
