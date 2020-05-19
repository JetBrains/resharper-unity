package com.jetbrains.rider.plugins.unity.debugger.breakpoints

object UnityPausepointConstants {
    const val pauseEditorCommand = "(UnityEditor.EditorApplication.isPaused = true) ? \"Unity pausepoint hit. Pausing Unity editor at the end of frame\" : \"Unable to pause Unity editor\""
    const val convertToPausepointText = "Convert to Unity pausepoint"
    const val convertToLineBreakpointText = "Convert to line breakpoint"
    const val unsupportedDebugTargetMessage = "Unity pausepoints are only available when debugging a Unity editor process"
    const val unavailableWhenNotPlayingMessage = "Unity pausepoints are only available in play mode"
}