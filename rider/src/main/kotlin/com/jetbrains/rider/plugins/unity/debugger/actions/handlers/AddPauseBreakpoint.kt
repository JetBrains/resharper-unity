package com.jetbrains.rider.plugins.unity.debugger.actions.handlers


import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.editor.LogicalPosition
import com.intellij.openapi.editor.ex.EditorGutterComponentEx
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.ui.ExperimentalUI.Companion.isNewUI
import com.intellij.xdebugger.XSourcePosition
import com.intellij.xdebugger.impl.XSourcePositionImpl
import com.intellij.xdebugger.impl.ui.DebuggerUIUtil
import com.jetbrains.rider.plugins.unity.actions.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.actions.valueOrDefault
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.addUnityPausepoint

class AddPauseBreakpoint : DumbAwareAction() {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val position = getLineBreakpointPosition(e)!!
        addUnityPausepoint(project, position.file, position.line)
    }

    override fun update(e: AnActionEvent) {
        val isUnityProject = e.isUnityProjectFolder
        e.presentation.setEnabledAndVisible(isUnityProject.valueOrDefault && isNewUI() && getLineBreakpointPosition(e) != null)
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
                        lineNumber = e.getData(DebuggerUIUtil.ACTIVE_LINE_NUMBER)
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

