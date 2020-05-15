package com.jetbrains.rider.plugins.unity.debugger.breakpoints

object UnityPausepointConstants {
    const val pauseEditorCommand = "(UnityEditor.EditorApplication.isPaused = true) ? \"Unity pausepoint hit. Pausing Unity editor at the end of frame\" : \"Unable to pause Unity editor\""
    const val convertToPausepointText = "Convert to Unity Pausepoint"
    const val convertToLineBreakpointText = "Convert to Line Breakpoint"
}