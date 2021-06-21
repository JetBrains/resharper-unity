package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import com.jetbrains.rider.debugger.breakpoint.IDotNetLineBreakpointPopupActionsProvider

class ConvertToPausepointPopupActionProvider : IDotNetLineBreakpointPopupActionsProvider {
    override fun getCustomPopupMenuActions(breakpoint: XLineBreakpoint<*>, session: XDebugSession?): List<AnAction> {
        if (breakpoint.type is DotNetLineBreakpointType && breakpoint.type !is UnityPausepointBreakpointType) {
            return listOf<AnAction>(ConvertToPausepointAction(breakpoint))
        }
        return emptyList()
    }

    private class ConvertToPausepointAction(private val breakpoint: XLineBreakpoint<*>): DumbAwareAction(UnityPausepointConstants.convertToPausepointText) {
        override fun update(e: AnActionEvent) {
            e.presentation.isVisible = e.project?.let { UnityProjectDiscoverer.getInstance(it).isUnityProject } == true
        }

        override fun actionPerformed(e: AnActionEvent) {
            val project = e.project ?: return
            val editor = e.getData(CommonDataKeys.EDITOR) ?: return

            @Suppress("UNCHECKED_CAST")
            convertToPausepoint(project, breakpoint as XLineBreakpoint<DotNetLineBreakpointProperties>, editor)
        }
    }
}
