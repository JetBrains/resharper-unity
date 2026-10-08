package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import java.nio.file.Path

fun unityMcpConfigureLocalCommandLine(unity: Path, solution: Path?): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "mcp", "configure", UNITY_MCP_CODEX_KEY, "--local", "--yes")
        .withWorkingDirectory(solution)

fun unityMcpWriteCommandLine(
    store: UnityMcpStore,
    operation: UnityMcpOperation,
    unity: Path,
    solution: Path?,
): GeneralCommandLine? = when (store) {
    UnityMcpStore.CODEX_CLI -> when (operation) {
        UnityMcpOperation.CONFIGURING -> unityMcpConfigureLocalCommandLine(unity, solution)
        UnityMcpOperation.REMOVING -> null
    }
    UnityMcpStore.CLAUDE_CODE_CLI, UnityMcpStore.COPILOT_CLI, UnityMcpStore.JUNIE_CLI -> null
}

fun unityMcpWriteConfirmed(operation: UnityMcpOperation, entry: UnityMcpEntry?): Boolean = when (operation) {
    UnityMcpOperation.CONFIGURING -> entry == UnityMcpEntry.PRESENT
    UnityMcpOperation.REMOVING -> entry == UnityMcpEntry.ABSENT
}

fun unityMcpWriteOutcome(output: ProcessOutput?, confirmed: Boolean): UnityMcpFailure? = when {
    output == null || output.isTimeout -> UnityMcpFailure.LAUNCH_FAILED
    confirmed -> null
    output.exitCode != 0 -> UnityMcpFailure.EXIT_CODE
    else -> UnityMcpFailure.NOT_CONFIRMED
}

fun unityMcpFileWriteOutcome(written: Boolean, confirmed: Boolean): UnityMcpFailure? = when {
    !written -> UnityMcpFailure.FILE_WRITE
    confirmed -> null
    else -> UnityMcpFailure.NOT_CONFIRMED
}

fun unityMcpRestartNeeded(
    failure: UnityMcpFailure?,
    before: UnityMcpEntry?,
    after: UnityMcpEntry?,
    heldRestart: Boolean,
    heldEntry: UnityMcpEntry?,
): Boolean {
    val carried = heldRestart && heldEntry != null && heldEntry == before
    val changed = failure == null && before.isKnown() && after.isKnown() && before != after
    return carried || changed
}

fun unityMcpHeldIsStale(
    heldEntry: UnityMcpEntry?,
    heldRelease: Long,
    entry: UnityMcpEntry?,
    readStart: Long,
): Boolean = heldEntry != null && heldRelease <= readStart && entry.isKnown() && entry != heldEntry

private fun UnityMcpEntry?.isKnown(): Boolean = this == UnityMcpEntry.ABSENT || this == UnityMcpEntry.PRESENT
