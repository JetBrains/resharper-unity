package com.jetbrains.rider.plugins.unity.debugger.actions.handlers


import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.editor.LogicalPosition
import com.intellij.openapi.editor.ex.EditorGutterComponentEx
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.ui.ExperimentalUI.Companion.isNewUI
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.XSourcePosition
import com.intellij.xdebugger.breakpoints.SuspendPolicy
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.intellij.xdebugger.impl.XSourcePositionImpl
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder

class AddPauseBreakpoint : DumbAwareAction() {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getRequiredData(CommonDataKeys.PROJECT)
        val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager

        val position = getLineBreakpointPosition(e)!!
        val unityPausepointType = XDebuggerUtil.getInstance().findBreakpointType(UnityPausepointBreakpointType::class.java)
        val properties = unityPausepointType.createBreakpointProperties(position.file, position.line)
        breakpointManager.addLineBreakpoint(unityPausepointType,
                                            position.file.url,
                                            position.line,
                                            properties)
            .apply {
                this.suspendPolicy = SuspendPolicy.NONE
            }
    }

    override fun update(e: AnActionEvent) {
        val isUnityProject = e.project?.isUnityProjectFolder()
        e.presentation.setEnabledAndVisible(isUnityProject == true && isNewUI() && getLineBreakpointPosition(e) != null)
    }

    override fun getActionUpdateThread(): ActionUpdateThread {
        return ActionUpdateThread.BGT
    }

    companion object {
        private fun getLineBreakpointPosition(e: AnActionEvent): XSourcePosition? {
            val project = e.project
            val editor = e.getData(CommonDataKeys.EDITOR)
            val file = e.getData(CommonDataKeys.VIRTUAL_FILE)
            if (project != null && editor != null && file != null) {
                val gutter = editor.getGutter()
                if (gutter is EditorGutterComponentEx) {
                    var lineNumber = gutter.getClientProperty("active.line.number")
                    if (lineNumber !is Int) {
                        lineNumber = e.getData(XDebuggerManagerImpl.ACTIVE_LINE_NUMBER)
                    }
                    if (lineNumber != null) {
                        val pos = LogicalPosition(
                            (lineNumber as Int), 0)
                        return XSourcePositionImpl.createByOffset(file, editor.logicalPositionToOffset(pos))
                    }
                }
            }
            return null
        }
    }
}

