// For DebuggerSupport
@file:Suppress("DEPRECATION")

package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.markup.GutterIconRenderer
import com.intellij.openapi.project.Project
import com.intellij.util.ui.UIUtil
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.SuspendPolicy
import com.intellij.xdebugger.breakpoints.XBreakpoint
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.intellij.xdebugger.impl.DebuggerSupport
import com.intellij.xdebugger.impl.XDebuggerSupport
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType

fun convertToPausepoint(project: Project, breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
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

fun convertToLineBreakpoint(project: Project, breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>, editor: Editor? = null, gutterIconRenderer: GutterIconRenderer? = null) {
    UIUtil.invokeLaterIfNeeded {
        application.runWriteAction {
            val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager
            val dotnetLineBreakpointType = XDebuggerUtil.getInstance().findBreakpointType(DotNetLineBreakpointType::class.java)
            breakpointManager.removeBreakpoint(breakpoint)

            val newBreakpoint = breakpointManager.addLineBreakpoint(dotnetLineBreakpointType, breakpoint.fileUrl, breakpoint.line, breakpoint.properties)
            if (editor != null && gutterIconRenderer != null) {
                editBreakpoint(project, editor, newBreakpoint, gutterIconRenderer)
            }
        }
    }
}

private fun editBreakpoint(project: Project, editor: Editor, breakpoint: XBreakpoint<*>, breakpointGutterRenderer: GutterIconRenderer) {
    // Use the default debugger support's edit action to show the edit balloon. This does a couple more things than
    // simply calling DebuggerUIUtil.showXBreakpointEditorBalloon, such as figure out where to show it, based on the
    // gutter icon renderer. This is what the Edit Breakpoint Alt+Enter action calls
    val debuggerSupport = DebuggerSupport.getDebuggerSupport(XDebuggerSupport::class.java)
    debuggerSupport.editBreakpointAction.editBreakpoint(project, editor, breakpoint, breakpointGutterRenderer)
}
