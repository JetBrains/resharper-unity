package com.jetbrains.rider.plugins.unity.settings.agents

import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.readText

private const val CODEX_STORE_FILE = "config.toml"
private const val MCP_SERVERS_TABLE = "mcp_servers"
private val CODEX_ENTRY = listOf(MCP_SERVERS_TABLE, UNITY_MCP_CODEX_SERVER_NAME)

fun unityMcpCodexStoreFile(solution: Path): Path =
    solution.resolve(UNITY_MCP_CODEX_DIRECTORY).resolve(CODEX_STORE_FILE)

suspend fun removeUnityMcpCodexEntryInIde(file: Path): Boolean =
    writeStoreInIde(file) { removeUnityMcpCodexEntry(file) }

fun removeUnityMcpCodexEntry(file: Path): Boolean {
    if (Files.notExists(file)) return true
    val content = file.readText()
    val stripped = unityMcpCodexStoreWithoutEntry(content) ?: return false
    if (stripped != content) replaceStoreFile(file, stripped)
    return true
}

/**
 * Drops the `[mcp_servers.unity]` table and its sub-tables, and keeps every other line as it is.
 *
 * The three sandbox tables that `unity mcp configure codex` also writes stay, as `codex mcp remove`
 * leaves them too. Rider cannot tell whether the user or another server needs them.
 *
 * Returns null for a file that this line scan cannot edit safely: an entry in a dotted key or an
 * inline table, or a header or a value that does not parse.
 */
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

private class TomlKey(val path: List<String>, val end: Int)

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
