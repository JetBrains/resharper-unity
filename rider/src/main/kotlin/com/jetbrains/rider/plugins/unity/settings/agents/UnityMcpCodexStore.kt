package com.jetbrains.rider.plugins.unity.settings.agents

import org.jetbrains.annotations.ApiStatus
import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.readText

private const val CODEX_STORE_FILE = "config.toml"
private const val MCP_SERVERS_TABLE = "mcp_servers"
private val CODEX_ENTRY = listOf(MCP_SERVERS_TABLE, UNITY_MCP_SERVER_NAME)

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
    val value = TomlValueScan()
    var table = emptyList<String>()
    var inEntry = false

    for (line in content.split(Regex("(?<=\n)"))) {
        if (value.isOpen) {
            value.scan(line)
        }
        else {
            val text = line.trim()
            if (text.startsWith("[")) {
                table = readTomlHeader(text) ?: return null
                inEntry = table.startsWith(CODEX_ENTRY)
            }
            else if (text.isNotEmpty() && !text.startsWith("#")) {
                val key = readTomlKey(text, 0) ?: return null
                val path = table + key.path
                if (!inEntry && (path.startsWith(CODEX_ENTRY) || path == listOf(MCP_SERVERS_TABLE))) return null
                val rest = text.substring(key.end)
                if (!rest.startsWith("=")) return null
                value.scan(rest.substring(1))
            }
        }
        if (!inEntry) kept.append(line)
    }
    return if (value.isOpen) null else kept.toString()
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
    val value = TomlValueScan()
    var section = TomlSection(policyTable = null, leading = "", header = null)

    for (line in content.split(Regex("(?<=\n)"))) {
        if (value.isOpen) {
            value.scan(line)
            section.keep(line)
            continue
        }
        val text = line.trim()
        when {
            text.isEmpty() -> section.blank(line)
            text.startsWith("[") -> {
                val table = readTomlHeader(text) ?: return null
                val policyTable = table.takeIf { it in UNITY_NETWORK_POLICY_TABLES && isPlainTomlHeader(text) }
                val leading = section.takeBlanks()
                section.appendTo(kept)
                section = TomlSection(policyTable, leading, line)
            }
            text.startsWith("#") -> section.keep(line)
            else -> {
                val key = readTomlKey(text, 0) ?: return null
                val rest = text.substring(key.end)
                if (!rest.startsWith("=")) return null
                val raw = rest.substring(1)
                if (section.isUnityPolicyKey(key.path, raw.trim())) section.drop() else section.keep(line)
                value.scan(raw)
            }
        }
    }
    if (value.isOpen) return null
    section.appendTo(kept)
    return kept.toString()
}

private class TomlKey(val path: List<String>, val end: Int)

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
