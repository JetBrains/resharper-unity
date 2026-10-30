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

private class NetworkPolicyKey(val table: List<String>, val key: String, val values: Set<String>) {
    /** The value that Rider writes. */
    val written: String get() = values.first()
}

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

/**
 * Adds the Unity entry and Codex's sandbox network policy to the Codex store at [file].
 *
 * Returns false when [unityMcpCodexStoreWithEntry] cannot edit the store safely.
 */
@ApiStatus.Internal
fun addUnityMcpCodexEntry(file: Path, unity: Path): Boolean {
    val content = if (Files.notExists(file)) "" else file.readText()
    val updated = unityMcpCodexStoreWithEntry(content, unity) ?: return false
    if (updated != content) replaceStoreFile(file, updated)
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
 * Codex's sandbox network policy, which [unityMcpCodexStoreWithEntry] also writes, stays, as `codex mcp remove`
 * leaves it too. [unityMcpCodexStoreWithoutNetworkPolicy] drops it.
 *
 * Returns null for a file that this line scan cannot edit safely.
 * Such a file holds the entry in a dotted key or an inline table, or a header or a value that does not parse.
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
 * Tells whether the store has the `[mcp_servers.unity]` table, or a dotted key under it.
 *
 * Returns null for a header or a value that does not parse, and for an inline `mcp_servers` table, which this line scan cannot read.
 */
@ApiStatus.Internal
fun hasUnityMcpCodexEntry(content: String): Boolean? {
    var found = false
    val parsed = walkTomlLines(content) { _, kind ->
        when (kind) {
            is TomlLine.Header -> if (kind.table.startsWith(CODEX_ENTRY)) found = true
            is TomlLine.KeyValue -> when {
                kind.path.startsWith(CODEX_ENTRY) -> found = true
                // An inline `mcp_servers` table can hold the entry, and this line scan cannot look inside it.
                kind.path == listOf(MCP_SERVERS_TABLE) -> return null
            }
            TomlLine.Blank, TomlLine.Comment, TomlLine.Continuation -> {}
        }
    }
    return if (parsed) found else null
}

/**
 * Drops the keys of Codex's sandbox network policy that [unityMcpCodexStoreWithEntry] writes, and keeps every other
 * line as it is.
 *
 * A key goes only under a table header that is not an array of tables, with a value of the policy and with no comment. A table
 * goes whole when it loses its last line, with the blank lines above its header. A header with a comment stays.
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
                val policyTable = kind.table.takeIf { it in UNITY_NETWORK_POLICY_TABLES && !kind.isArray }
                val leading = section.takeBlanks()
                section.appendTo(kept)
                section = TomlSection(policyTable, leading, line, keepsHeader = !kind.isPlain)
            }
            is TomlLine.KeyValue -> if (section.isUnityPolicyKey(kind.key, kind.value)) section.drop() else section.keep(line)
        }
    }
    if (!parsed) return null
    section.appendTo(kept)
    return kept.toString()
}

/**
 * Adds the `[mcp_servers.unity]` table and Codex's sandbox network policy, and keeps every other line as it is.
 *
 * Rider drops the old entry first. A policy key goes under the header of its table. Rider keeps a line with a value of
 * the policy, and replaces another value. A missing key goes after the last key of the table. A policy table with no
 * header goes to the end of the store, and the entry goes after it.
 *
 * Returns null for a store that this line scan cannot edit safely:
 * - a policy key in a dotted key or an inline table;
 * - a policy table in an array of tables;
 * - an old entry that [unityMcpCodexStoreWithoutEntry] cannot drop;
 * - a header or a value that does not parse.
 */
@ApiStatus.Internal
fun unityMcpCodexStoreWithEntry(content: String, unity: Path): String? {
    val newline = if ("\r\n" in content) "\r\n" else "\n"
    var text = unityMcpCodexStoreWithoutEntry(content) ?: return null
    for (table in UNITY_NETWORK_POLICY_TABLES) {
        text = withTomlPolicyTable(text, table, newline) ?: return null
    }
    return appendTomlTable(text, unityMcpCodexEntry(unity).lines(), newline)
}

private class TomlEntry(val text: String, val kind: TomlLine)

private fun readTomlEntries(content: String): List<TomlEntry>? {
    val entries = mutableListOf<TomlEntry>()
    return if (walkTomlLines(content) { line, kind -> entries += TomlEntry(line, kind) }) entries else null
}

/** Sets the keys of the policy [table], or appends the table when it has no header. */
private fun withTomlPolicyTable(content: String, table: List<String>, newline: String): String? {
    val entries = readTomlEntries(content) ?: return null
    val keys = UNITY_NETWORK_POLICY.filter { it.table == table }
    if (entries.any { !isSafePolicyLine(it.kind, table, keys) }) return null
    val header = entries.indexOfFirst { (it.kind as? TomlLine.Header)?.table == table }
    if (header < 0) return appendTomlTable(content, listOf("[${tomlKeyPath(table)}]") + keys.map(::tomlPolicyLine), newline)

    val end = (header + 1 until entries.size).firstOrNull { entries[it].kind is TomlLine.Header } ?: entries.size
    val lines = entries.mapTo(mutableListOf()) { it.text }
    val missing = mutableListOf<String>()
    for (key in keys) {
        val index = (header + 1 until end).firstOrNull { (entries[it].kind as? TomlLine.KeyValue)?.key == listOf(key.key) }
        if (index == null) {
            missing += tomlPolicyLine(key) + newline
            continue
        }
        val value = (entries[index].kind as TomlLine.KeyValue).value
        if (value.substringBefore('#').trim() in key.values) continue
        // A value that spans lines leaves its other lines behind, so Rider does not replace it.
        if (entries.getOrNull(index + 1)?.kind == TomlLine.Continuation) return null
        val line = lines[index]
        lines[index] = line.takeWhile { it == ' ' || it == '\t' } + tomlPolicyLine(key) + lineEnding(line)
    }
    if (missing.isNotEmpty()) {
        val last = (header + 1 until end).lastOrNull {
            entries[it].kind is TomlLine.KeyValue || entries[it].kind == TomlLine.Continuation
        } ?: header
        if (!lines[last].endsWith("\n")) lines[last] += newline
        lines.addAll(last + 1, missing)
    }
    return lines.joinToString("")
}

/**
 * Tells whether [kind] leaves the policy [table] to table headers and plain keys.
 *
 * A dotted key or an inline table can define the table or one of its [keys] in another way. A line scan cannot edit
 * that safely. An array of tables above the table counts as user content. A comment after a header is safe.
 */
private fun isSafePolicyLine(kind: TomlLine, table: List<String>, keys: List<NetworkPolicyKey>): Boolean = when (kind) {
    is TomlLine.Header -> when {
        table.startsWith(kind.table) -> !kind.isArray
        else -> keys.none { kind.table.startsWith(table + it.key) }
    }
    is TomlLine.KeyValue -> when {
        kind.table == table -> kind.key.size == 1 || keys.none { it.key == kind.key.first() }
        kind.table.startsWith(table) -> true
        else -> !kind.path.startsWith(table) && !table.startsWith(kind.path)
    }
    TomlLine.Blank, TomlLine.Comment, TomlLine.Continuation -> true
}

/** Appends [table] to [content], with one blank line before it. */
private fun appendTomlTable(content: String, table: List<String>, newline: String): String {
    val text = StringBuilder(content)
    if (text.isNotEmpty() && !text.endsWith("\n")) text.append(newline)
    if (text.isNotBlank() && !text.endsWith("\n\n") && !text.endsWith("\r\n\r\n")) text.append(newline)
    table.forEach { text.append(it).append(newline) }
    return text.toString()
}

private fun tomlPolicyLine(key: NetworkPolicyKey): String = "${tomlKey(key.key)} = ${key.written}"

private fun tomlKeyPath(path: List<String>): String = path.joinToString(".", transform = ::tomlKey)

private fun tomlKey(key: String): String = if (key.isNotEmpty() && key.all(::isBareKeyChar)) key else "\"$key\""

private fun lineEnding(line: String): String = when {
    line.endsWith("\r\n") -> "\r\n"
    line.endsWith("\n") -> "\n"
    else -> ""
}

private class TomlKey(val path: List<String>, val end: Int)

/** What one line of the store holds. */
private sealed interface TomlLine {
    /** A line inside a value that spans lines. */
    data object Continuation : TomlLine
    data object Blank : TomlLine
    data object Comment : TomlLine
    class Header(val table: List<String>, val isPlain: Boolean, val isArray: Boolean) : TomlLine
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
    text.startsWith("[") -> readTomlHeader(text)?.let { TomlLine.Header(it, isPlainTomlHeader(text), isArray = text.startsWith("[[")) }
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
 * A header with a comment stays, because the comment belongs to the user.
 */
private class TomlSection(
    private val policyTable: List<String>?,
    leading: String,
    header: String?,
    private val keepsHeader: Boolean = false,
) {
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
        if (dropped && !kept && !keepsHeader) return
        out.append(text).append(blanks)
    }
}

/** A `[table]` header with no comment after it. A comment marks a header that the user wrote. */
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
