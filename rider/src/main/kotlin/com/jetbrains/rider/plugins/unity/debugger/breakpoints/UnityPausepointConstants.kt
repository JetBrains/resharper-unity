package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.util.NlsActions.ActionText
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls

object UnityPausepointConstants {
    @get:ActionText
    val convertToPausepointActionText = UnityBundle.message("convert.to.unity.pausepoint.action.text")

    @Nls(capitalization = Nls.Capitalization.Sentence)
    val convertToPausepointLabelLinkText = UnityBundle.message("convert.to.unity.pausepoint.label.link")

    @Nls(capitalization = Nls.Capitalization.Title)
    val convertToLineBreakpointActionText = UnityBundle.message("action.text.convert.to.line.breakpoint")

    @Nls(capitalization = Nls.Capitalization.Sentence)
    val convertToLineBreakpointLabelLinkText = UnityBundle.message("label.link.convert.to.line.breakpoint")
    const val unsupportedDebugTargetMessage = "Unity pausepoints are only available when debugging a Unity editor process"
    const val unavailableWhenNotPlayingMessage = "Unity pausepoints are only available in play mode"
}