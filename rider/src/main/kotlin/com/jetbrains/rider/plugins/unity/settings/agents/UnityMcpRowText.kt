package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.icons.AllIcons
import com.intellij.ui.AnimatedIcon
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls
import java.nio.file.Path
import javax.swing.Icon

internal data class UnityMcpRowStatus(val icon: Icon?, @param:Nls val text: String, val isProblem: Boolean)

internal fun unityMcpStoreName(store: UnityMcpStore): @Nls String = when (store) {
    UnityMcpStore.CODEX_CLI -> UnityBundle.message("unity.agents.mcp.store.codex")
    UnityMcpStore.CLAUDE_CODE_CLI -> UnityBundle.message("unity.agents.mcp.store.claude.code")
    UnityMcpStore.COPILOT_CLI -> UnityBundle.message("unity.agents.mcp.store.copilot")
    UnityMcpStore.JUNIE_CLI -> UnityBundle.message("unity.agents.mcp.store.junie")
}

@Nls
internal fun unityMcpSharedRemovalQuestion(stores: List<UnityMcpStore>, solution: Path?): String? {
    val others = unityMcpUnnamedSharers(stores)
    val first = others.firstOrNull() ?: return null
    val file = solution?.let { unityMcpJsonStoreFile(first, it) } ?: return null
    return UnityBundle.message(
        "unity.agents.mcp.remove.shared",
        others.joinToString(", ") { unityMcpStoreName(it) },
        file.toString(),
    )
}

internal fun unityMcpGateMessage(section: UnityMcpSectionState): @Nls String? = when (section) {
    UnityMcpSectionState.NO_PROJECT -> UnityBundle.message("unity.agents.mcp.no.project")
    UnityMcpSectionState.NOT_TRUSTED -> UnityBundle.message("unity.agents.mcp.not.trusted")
    UnityMcpSectionState.NO_CLI -> UnityBundle.message("unity.agents.mcp.needs.cli")
    UnityMcpSectionState.CHECKING, UnityMcpSectionState.READY -> null
}

internal fun unityMcpRowStatus(row: UnityMcpRowPresentation): UnityMcpRowStatus = when (row.state) {
    UnityMcpRowState.BUSY -> busy(row.operation)
    UnityMcpRowState.FAILED -> problem(unityMcpWriteFailureMessage(row.failure, row.failureExitCode))
    UnityMcpRowState.UNAVAILABLE -> plain(null, UnityBundle.message("unity.agents.mcp.state.unavailable"))
    UnityMcpRowState.CHECKING -> plain(AnimatedIcon.Default.INSTANCE, UnityBundle.message("unity.agents.mcp.state.checking"))
    UnityMcpRowState.UNREADABLE -> problem(UnityBundle.message("unity.agents.mcp.state.unreadable"))
    UnityMcpRowState.RESTART_NEEDED -> ok(restartMessage(unityMcpRestartLine(row.entry)))
    UnityMcpRowState.ENTRY_PRESENT -> ok(UnityBundle.message("unity.agents.mcp.state.configured"))
    UnityMcpRowState.NOT_DETECTED -> plain(null, UnityBundle.message("unity.agents.mcp.state.not.detected"))
    UnityMcpRowState.NOT_CONFIGURED -> plain(null, UnityBundle.message("unity.agents.mcp.state.not.configured"))
}

@Nls
private fun restartMessage(line: UnityMcpRestartLine): String = when (line) {
    UnityMcpRestartLine.CONFIGURED -> UnityBundle.message("unity.agents.mcp.state.restart.agent")
    UnityMcpRestartLine.REMOVED -> UnityBundle.message("unity.agents.mcp.state.restart.removed")
}

internal fun unityMcpRowDetail(row: UnityMcpRowPresentation): @Nls String? = row.storePath

@Nls
internal fun unityMcpPipelineConfirmation(manifestPath: String): String =
    UnityBundle.message("unity.agents.mcp.pipeline.confirm", manifestPath)

@Nls
internal fun unityMcpWriteFailureMessage(failure: UnityMcpFailure?, exitCode: Int?): String = when (failure) {
    UnityMcpFailure.LAUNCH_FAILED -> UnityBundle.message("unity.agents.mcp.failed.launch")
    UnityMcpFailure.EXIT_CODE -> UnityBundle.message("unity.agents.mcp.failed.exit.code", exitCode ?: -1)
    UnityMcpFailure.FILE_WRITE -> UnityBundle.message("unity.agents.mcp.failed.write")
    UnityMcpFailure.PACKAGE_INSTALL -> UnityBundle.message("unity.agents.mcp.failed.package")
    UnityMcpFailure.PACKAGE_UNREADABLE -> UnityBundle.message("unity.agents.mcp.failed.manifest")
    UnityMcpFailure.NOT_CONFIRMED, null -> UnityBundle.message("unity.agents.mcp.failed.not.confirmed")
}

private fun busy(operation: UnityMcpOperation?): UnityMcpRowStatus {
    val text = when (operation) {
        UnityMcpOperation.REMOVING -> UnityBundle.message("unity.agents.mcp.state.removing")
        UnityMcpOperation.CONFIGURING, null -> UnityBundle.message("unity.agents.mcp.state.configuring")
    }
    return plain(AnimatedIcon.Default.INSTANCE, text)
}

private fun ok(@Nls text: String) = UnityMcpRowStatus(AllIcons.General.InspectionsOK, text, isProblem = false)

private fun problem(@Nls text: String) = UnityMcpRowStatus(AllIcons.General.Error, text, isProblem = true)

private fun plain(icon: Icon?, @Nls text: String) = UnityMcpRowStatus(icon, text, isProblem = false)
