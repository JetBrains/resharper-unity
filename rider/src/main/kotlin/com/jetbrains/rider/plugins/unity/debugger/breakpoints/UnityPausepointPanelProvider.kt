package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.project.Project
import com.intellij.ui.HyperlinkLabel
import com.intellij.util.ui.UIUtil
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.SuspendPolicy
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.intellij.xdebugger.breakpoints.ui.XBreakpointCustomPropertiesPanel
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import com.jetbrains.rider.debugger.breakpoint.IDotNetLineBreakpointCustomPanelsProvider
import javax.swing.JComponent
import javax.swing.event.HyperlinkListener

class UnityPausepointPanelProvider : IDotNetLineBreakpointCustomPanelsProvider {
    override fun getCustomBreakpointPanel(project: Project): XBreakpointCustomPropertiesPanel<XLineBreakpoint<DotNetLineBreakpointProperties>>? {
        if (!UnityProjectDiscoverer.getInstance(project).isUnityProject)
            return null
        return UnityPausepointPanel(project)
    }
}

class UnityPausepointPanel(private val project: Project) : XBreakpointCustomPropertiesPanel<XLineBreakpoint<DotNetLineBreakpointProperties>>() {

    private val activatePausepointHyperlink = HyperlinkLabel(UnityPausepointConstants.convertToPausepointText)
    private var currentListener: HyperlinkListener? = null

    override fun loadFrom(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
        val isPausepoint = breakpoint.type is UnityPausepointBreakpointType
        activatePausepointHyperlink.setHyperlinkText(
            if (isPausepoint)
                UnityPausepointConstants.convertToLineBreakpointText
            else
                UnityPausepointConstants.convertToPausepointText
        )

        val listener = HyperlinkListener {
            if (!isPausepoint) {
                convertToPausepoint(breakpoint)
            } else {
                convertToLineBreakpoint(breakpoint)
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

    private fun convertToPausepoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
        UIUtil.invokeLaterIfNeeded {
            application.runWriteAction {
                val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager
                val unityPausepointType = XDebuggerUtil.getInstance().findBreakpointType(UnityPausepointBreakpointType::class.java)
                breakpointManager.removeBreakpoint(breakpoint)

                breakpointManager.addLineBreakpoint(unityPausepointType, breakpoint.fileUrl, breakpoint.line, breakpoint.properties).apply {
                    this.suspendPolicy = SuspendPolicy.NONE
                    this.logExpression = UnityPausepointConstants.pauseEditorCommand
                }
            }
        }
    }

    private fun convertToLineBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
        UIUtil.invokeLaterIfNeeded {
            application.runWriteAction {
                val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager
                val dotnetLineBreakpointType = XDebuggerUtil.getInstance().findBreakpointType(DotNetLineBreakpointType::class.java)
                breakpointManager.removeBreakpoint(breakpoint)

                breakpointManager.addLineBreakpoint(dotnetLineBreakpointType, breakpoint.fileUrl, breakpoint.line, breakpoint.properties)
            }
        }
    }
}