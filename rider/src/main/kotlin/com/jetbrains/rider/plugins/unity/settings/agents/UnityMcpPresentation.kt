package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.NlsSafe

enum class UnityMcpStore {
    /** `<solution>/.codex/config.toml`. The Unity CLI adds the entry `unity`, and Rider removes it. */
    CODEX_CLI,

    /**
     * `<solution>/.mcp.json`. Rider writes it, as `unity-editor-mcp`.
     *
     * [COPILOT_CLI] reads the same file, so the two rows always report the same state and a removal
     * on either one unconfigures both. They draw side by side for that reason.
     */
    CLAUDE_CODE_CLI,
    COPILOT_CLI,

    /** `<solution>/.junie/mcp/mcp.json`. Rider writes it, as `unity-editor-mcp` */
    JUNIE_CLI,
}

enum class UnityMcpOperation {
    CONFIGURING,
    REMOVING
}

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

enum class UnityMcpEntry {
    ABSENT,
    PRESENT,
    UNREADABLE,
}

/**
 * Which restart line a row shows.
 *
 * `UnityMcpRowState.RESTART_NEEDED` covers a configure and a removal, because a client keeps the
 * server it already loaded either way. The two cannot share one sentence.
 */
enum class UnityMcpRestartLine {
    CONFIGURED,
    REMOVED,
}

fun unityMcpRestartLine(entry: UnityMcpEntry?): UnityMcpRestartLine =
    if (entry == UnityMcpEntry.ABSENT) UnityMcpRestartLine.REMOVED else UnityMcpRestartLine.CONFIGURED

enum class UnityMcpRowState {
    BUSY,
    FAILED,
    UNAVAILABLE,
    CHECKING,
    UNREADABLE,
    RESTART_NEEDED,
    ENTRY_PRESENT,
    NOT_DETECTED,
    NOT_CONFIGURED,
}

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
    val agentDetected: Boolean = true,
    val restartNeeded: Boolean = false,
    val operation: UnityMcpOperation? = null,
    val failure: UnityMcpFailure? = null,
    val failureExitCode: Int? = null,
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
            // Only UnityMcpEntry.ABSENT reaches this line, so NOT_DETECTED holds both facts it needs.
            !agentDetected -> UnityMcpRowState.NOT_DETECTED
            else -> UnityMcpRowState.NOT_CONFIGURED
        }
}

fun unityMcpSectionState(cli: UnityCliPresentation, unityProject: Boolean): UnityMcpSectionState = when {
    !unityProject -> UnityMcpSectionState.NO_PROJECT
    !cli.projectTrusted -> UnityMcpSectionState.NOT_TRUSTED
    !cli.detected -> UnityMcpSectionState.CHECKING
    cli.location == null -> UnityMcpSectionState.NO_CLI
    else -> UnityMcpSectionState.READY
}

data class UnityMcpProject(val name: @NlsSafe String, val path: @NlsSafe String)

data class UnityMcpSectionPresentation(
    val cli: UnityCliPresentation = UnityCliPresentation(),
    val project: UnityMcpProject? = null,
    val rows: List<UnityMcpRowPresentation> = emptyList(),
    val manifestPath: @NlsSafe String? = null,
    val pipelinePackage: UnityPipelineManifest? = null,
) {
    val state: UnityMcpSectionState get() = unityMcpSectionState(cli, project != null)
    val needsPipelinePackage: Boolean? get() = pipelinePackage?.present?.not()
}

fun unityMcpCanRemove(row: UnityMcpRowPresentation): Boolean =
    row.state != UnityMcpRowState.BUSY && row.entry != UnityMcpEntry.ABSENT

fun unityMcpOpenableStore(row: UnityMcpRowPresentation): String? =
    row.storePath?.takeIf { row.storeFileExists }
