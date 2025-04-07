// For DebuggerSupport. Deprecated in 2015, but still in use, and no alternatives
@file:Suppress("DEPRECATION")

package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.EditorEx
import com.intellij.openapi.editor.markup.GutterIconRenderer
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.TextEditor
import com.intellij.openapi.project.Project
import com.intellij.util.application
import com.intellij.util.ui.UIUtil
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.SuspendPolicy
import com.intellij.xdebugger.breakpoints.XBreakpoint
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.intellij.xdebugger.impl.DebuggerSupport
import com.intellij.xdebugger.impl.breakpoints.XBreakpointManagerImpl
import com.intellij.xdebugger.impl.breakpoints.XBreakpointUtil
import com.intellij.xdebugger.impl.breakpoints.ui.BreakpointsDialogFactory
import com.intellij.xdebugger.impl.ui.DebuggerUIUtil
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import java.awt.Point

fun convertToPausepoint(project: Project,
                        breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>,
                        providedEditor: Editor? = null,
                        providedIconRenderer: GutterIconRenderer? = null) {
    UIUtil.invokeLaterIfNeeded {
        application.runWriteAction {
            val balloonLocation = tryGetIconRendererLocation(project, providedEditor, breakpoint, providedIconRenderer)

            val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager
            val dependentBreakpointManager = (breakpointManager as? XBreakpointManagerImpl)?.dependentBreakpointManager
            val masterBreakpoint = dependentBreakpointManager?.getMasterBreakpoint(breakpoint)
            val leaveEnabled = dependentBreakpointManager?.isLeaveEnabled(breakpoint) ?: false
            breakpointManager.removeBreakpoint(breakpoint)

            val unityPausepointType = XDebuggerUtil.getInstance().findBreakpointType(UnityPausepointBreakpointType::class.java)
            val newBreakpoint = breakpointManager.addLineBreakpoint(unityPausepointType, breakpoint.fileUrl, breakpoint.line,
                                                                    breakpoint.properties).apply {
                this.suspendPolicy = SuspendPolicy.NONE

                // Copy over condition + dependent breakpoint details. Hit count is automatically copied from properties
                this.conditionExpression = breakpoint.conditionExpression

                if (masterBreakpoint != null) {
                    dependentBreakpointManager.setMasterBreakpoint(this, masterBreakpoint, leaveEnabled)
                }
            }

            tryEditBreakpoint(project, newBreakpoint, balloonLocation, providedEditor)
        }
    }
}

fun convertToLineBreakpoint(project: Project,
                            breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>,
                            providedEditor: Editor? = null,
                            providedIconRenderer: GutterIconRenderer? = null) {
    UIUtil.invokeLaterIfNeeded {
        application.runWriteAction {
            val balloonLocation = tryGetIconRendererLocation(project, providedEditor, breakpoint, providedIconRenderer)

            val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager
            val dependentBreakpointManager = (breakpointManager as? XBreakpointManagerImpl)?.dependentBreakpointManager
            val masterBreakpoint = dependentBreakpointManager?.getMasterBreakpoint(breakpoint)
            val leaveEnabled = dependentBreakpointManager?.isLeaveEnabled(breakpoint) ?: false
            breakpointManager.removeBreakpoint(breakpoint)

            val dotnetLineBreakpointType = XDebuggerUtil.getInstance().findBreakpointType(DotNetLineBreakpointType::class.java)
            val newBreakpoint = breakpointManager.addLineBreakpoint(dotnetLineBreakpointType, breakpoint.fileUrl, breakpoint.line,
                                                                    breakpoint.properties).apply {
                // Copy over condition + dependent breakpoint details. Hit count is automatically copied from properties
                this.conditionExpression = breakpoint.conditionExpression

                if (masterBreakpoint != null) {
                    dependentBreakpointManager.setMasterBreakpoint(this, masterBreakpoint, leaveEnabled)
                }
            }

            tryEditBreakpoint(project, newBreakpoint, balloonLocation, providedEditor)
        }
    }
}

fun tryGetIconRendererLocation(project: Project,
                               providedEditor: Editor?,
                               breakpoint: XLineBreakpoint<*>,
                               providedIconRenderer: GutterIconRenderer?): Point? {
    val editor = tryGetEditor(project, providedEditor) ?: return null
    val renderer = tryGetGutterIconRenderer(breakpoint, providedIconRenderer) ?: return null

    return (editor as? EditorEx)?.gutterComponentEx?.getCenterPoint(renderer)
}

private fun tryGetEditor(project: Project, providedEditor: Editor?): Editor? {
    if (providedEditor != null) return providedEditor
    return (FileEditorManager.getInstance(project).selectedEditor as? TextEditor)?.editor
}

private fun tryGetGutterIconRenderer(breakpoint: XBreakpoint<*>, providedIconRenderer: GutterIconRenderer?): GutterIconRenderer? {
    if (providedIconRenderer != null) return providedIconRenderer

    return XBreakpointUtil.getBreakpointGutterIconRenderer(breakpoint)
}

private fun tryEditBreakpoint(project: Project, breakpoint: XBreakpoint<*>, whereToShow: Point?, providedEditor: Editor?) {
    val editor = tryGetEditor(project, providedEditor) ?: return

    // Don't show the balloon if the dialog is already open
    if (!BreakpointsDialogFactory.getInstance(project).popupRequested(breakpoint)) {
        val gutterComponent = (editor as? EditorEx)?.gutterComponentEx ?: return
        DebuggerUIUtil.showXBreakpointEditorBalloon(project, whereToShow, gutterComponent, false, breakpoint)
    }
}

