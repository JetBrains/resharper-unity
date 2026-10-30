package com.jetbrains.rider.plugins.unity.settings.agents

import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
fun unityMcpWriteConfirmed(operation: UnityMcpOperation, entry: UnityMcpEntry?): Boolean = when (operation) {
    UnityMcpOperation.CONFIGURING -> entry == UnityMcpEntry.PRESENT
    UnityMcpOperation.REMOVING -> entry == UnityMcpEntry.ABSENT
}

@ApiStatus.Internal
fun unityMcpFileWriteOutcome(written: Boolean, confirmed: Boolean): UnityMcpFailure? = when {
    !written -> UnityMcpFailure.FILE_WRITE
    confirmed -> null
    else -> UnityMcpFailure.NOT_CONFIRMED
}

@ApiStatus.Internal
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

@ApiStatus.Internal
fun unityMcpHeldIsStale(
    heldEntry: UnityMcpEntry?,
    heldRelease: Long,
    entry: UnityMcpEntry?,
    readStart: Long,
): Boolean = heldEntry != null && heldRelease <= readStart && entry.isKnown() && entry != heldEntry

private fun UnityMcpEntry?.isKnown(): Boolean = this != null && this != UnityMcpEntry.UNREADABLE
