package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.NlsSafe
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
enum class UnityMcpStore {
    /** `<solution>/.codex/config.toml`. The Unity CLI adds the entry `unity` and Codex's sandbox network policy, and Rider removes them. */
    CODEX_CLI,

    /**
     * `<solution>/.mcp.json`. Rider writes it, as `unity`.
     *
     * [COPILOT_CLI] reads the same file, so the two rows always report the same state and a removal
     * on either one unconfigures both. They draw side by side for that reason.
     */
    CLAUDE_CODE_CLI,
    COPILOT_CLI,

    /** `<solution>/.junie/mcp/mcp.json`. Rider writes it, as `unity` */
    JUNIE_CLI,
}

@ApiStatus.Internal
enum class UnityMcpOperation {
    CONFIGURING,
    REMOVING
}

@ApiStatus.Internal
enum class UnityMcpFailure {
    LAUNCH_FAILED,
    EXIT_CODE,
    PACKAGE_INSTALL,

    /**
     * The manifest of the Unity project did not answer, so the write stopped before it began.
     *
     * [PACKAGE_INSTALL] cannot carry this. It says an install failed, and nothing was installed.
     */
    PACKAGE_UNREADABLE,
    FILE_WRITE,
    NOT_CONFIRMED,
}

@ApiStatus.Internal
enum class UnityMcpSectionState {
    NO_PROJECT,
    CHECKING,
    NOT_TRUSTED,
    NO_CLI,
    READY,
    ;

    val canAct: Boolean get() = this == READY
    val drawsBody: Boolean get() = this != NO_PROJECT
}

@ApiStatus.Internal
enum class UnityMcpEntry {
    ABSENT,
    PRESENT,
    OTHER_CLI,
    UNREADABLE,
}

/**
 * Which restart line a row shows.
 *
 * `UnityMcpRowState.RESTART_NEEDED` covers a configure and a removal, because a client keeps the
 * server it already loaded either way. The two cannot share one sentence.
 */
@ApiStatus.Internal
enum class UnityMcpRestartLine {
    CONFIGURED,
    REMOVED,
}

@ApiStatus.Internal
fun unityMcpRestartLine(entry: UnityMcpEntry?): UnityMcpRestartLine =
    if (entry == UnityMcpEntry.ABSENT) UnityMcpRestartLine.REMOVED else UnityMcpRestartLine.CONFIGURED

@ApiStatus.Internal
enum class UnityMcpRowState {
    BUSY,
    FAILED,
    UNAVAILABLE,
    CHECKING,
    UNREADABLE,
    RESTART_NEEDED,
    ENTRY_PRESENT,
    OTHER_CLI,
    NOT_CONFIGURED,
}

@ApiStatus.Internal
data class UnityMcpRowPresentation(
    val store: UnityMcpStore,
    val section: UnityMcpSectionState = UnityMcpSectionState.CHECKING,
    val storePath: @NlsSafe String? = null,
    /**
     * Whether a file sits at [storePath] today.
     *
     * The path of a JSON row comes from the solution and not from the store, so it names a file that
     * the first write creates. The read already answers this, so nothing stats the file on the EDT.
     */
    val storeFileExists: Boolean = false,
    val entry: UnityMcpEntry? = null,
    val restartNeeded: Boolean = false,
    val operation: UnityMcpOperation? = null,
    val failure: UnityMcpFailure? = null,
    val failureExitCode: Int? = null,
    /** The Codex store holds other content next to Codex's sandbox network policy, so a removal asks the user. */
    val removalAsksForNetworkPolicy: Boolean = false,
    /** The JSON store holds another server named `unity`, so a configure asks the user before it replaces it. */
    val configureReplacesOtherServer: Boolean = false,
) {
    val state: UnityMcpRowState
        get() = when {
            operation != null -> UnityMcpRowState.BUSY
            failure != null -> UnityMcpRowState.FAILED
            !section.canAct && section != UnityMcpSectionState.CHECKING -> UnityMcpRowState.UNAVAILABLE
            section == UnityMcpSectionState.CHECKING || entry == null -> UnityMcpRowState.CHECKING
            entry == UnityMcpEntry.UNREADABLE -> UnityMcpRowState.UNREADABLE
            restartNeeded -> UnityMcpRowState.RESTART_NEEDED
            entry == UnityMcpEntry.PRESENT -> UnityMcpRowState.ENTRY_PRESENT
            entry == UnityMcpEntry.OTHER_CLI -> UnityMcpRowState.OTHER_CLI
            else -> UnityMcpRowState.NOT_CONFIGURED
        }
}

@ApiStatus.Internal
fun unityMcpSectionState(cli: UnityCliPresentation, unityProject: Boolean): UnityMcpSectionState = when {
    !unityProject -> UnityMcpSectionState.NO_PROJECT
    !cli.projectTrusted -> UnityMcpSectionState.NOT_TRUSTED
    !cli.detected -> UnityMcpSectionState.CHECKING
    cli.location == null -> UnityMcpSectionState.NO_CLI
    else -> UnityMcpSectionState.READY
}

internal data class UnityMcpProject(val name: @NlsSafe String, val path: @NlsSafe String)

internal data class UnityMcpSectionPresentation(
    val cli: UnityCliPresentation = UnityCliPresentation(),
    val project: UnityMcpProject? = null,
    val rows: List<UnityMcpRowPresentation> = emptyList(),
) {
    val state: UnityMcpSectionState get() = unityMcpSectionState(cli, project != null)
}

@ApiStatus.Internal
fun unityMcpCanConfigure(row: UnityMcpRowPresentation): Boolean = when (row.state) {
    UnityMcpRowState.BUSY -> false
    UnityMcpRowState.UNREADABLE -> unityMcpJsonStoreName(row.store) == null
    UnityMcpRowState.FAILED,
    UnityMcpRowState.UNAVAILABLE,
    UnityMcpRowState.CHECKING,
    UnityMcpRowState.RESTART_NEEDED,
    UnityMcpRowState.ENTRY_PRESENT,
    UnityMcpRowState.OTHER_CLI,
    UnityMcpRowState.NOT_CONFIGURED,
        -> true
}

@ApiStatus.Internal
fun unityMcpCanRemove(row: UnityMcpRowPresentation): Boolean =
    row.state != UnityMcpRowState.BUSY && row.entry != UnityMcpEntry.ABSENT

@ApiStatus.Internal
fun unityMcpOpenableStore(row: UnityMcpRowPresentation): String? =
    row.storePath?.takeIf { row.storeFileExists }
