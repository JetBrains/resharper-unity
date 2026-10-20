package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml

import com.jetbrains.rider.editorActions.RiderTypingPolicy

/**
 * Runs every typed character in a UXML file on the frontend.
 */
class UxmlTypingPolicy : RiderTypingPolicy {
    override fun forceFrontendExecution(): Boolean = true
}
