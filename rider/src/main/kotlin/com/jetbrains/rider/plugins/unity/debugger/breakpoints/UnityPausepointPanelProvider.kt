package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.project.Project
import com.intellij.ui.HyperlinkLabel
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.intellij.xdebugger.breakpoints.ui.XBreakpointCustomPropertiesPanel
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.IDotNetLineBreakpointCustomPanelsProvider
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.isUnityProject
import javax.swing.JComponent
import javax.swing.event.HyperlinkListener

class UnityPausepointPanelProvider : IDotNetLineBreakpointCustomPanelsProvider {
    override fun getCustomBreakpointPanel(project: Project): XBreakpointCustomPropertiesPanel<XLineBreakpoint<DotNetLineBreakpointProperties>>? {
        if (!project.isUnityProject.getCompletedOr(false))
            return null
        return UnityPausepointPanel(project)
    }
}

class UnityPausepointPanel(private val project: Project) : XBreakpointCustomPropertiesPanel<XLineBreakpoint<DotNetLineBreakpointProperties>>() {

    private val activatePausepointHyperlink = HyperlinkLabel(UnityPausepointConstants.convertToPausepointLabelLinkText)
    private var currentListener: HyperlinkListener? = null

    override fun loadFrom(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
        val isPausepoint = breakpoint.type is UnityPausepointBreakpointType
        activatePausepointHyperlink.setHyperlinkText(
            if (isPausepoint)
                UnityPausepointConstants.convertToLineBreakpointLabelLinkText
            else
                UnityPausepointConstants.convertToPausepointLabelLinkText
        )

        val listener = HyperlinkListener {
            if (!isPausepoint) {
                convertToPausepoint(project, breakpoint)
            } else {
                convertToLineBreakpoint(project, breakpoint)
            }
        }

        if (currentListener != null) {
            activatePausepointHyperlink.removeHyperlinkListener(currentListener)
        }
        currentListener = listener

        activatePausepointHyperlink.addHyperlinkListener(listener)
    }

    override fun saveTo(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
    }

    override fun getComponent(): JComponent = activatePausepointHyperlink
}