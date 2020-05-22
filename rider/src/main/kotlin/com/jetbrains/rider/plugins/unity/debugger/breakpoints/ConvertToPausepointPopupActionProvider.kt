package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import com.jetbrains.rider.debugger.breakpoint.IDotNetLineBreakpointPopupActionsProvider

class ConvertToPausepointPopupActionProvider : IDotNetLineBreakpointPopupActionsProvider {
    override fun getCustomPopupMenuActions(breakpoint: XLineBreakpoint<*>, session: XDebugSession?): List<AnAction> {
        if (breakpoint.type is DotNetLineBreakpointType && breakpoint.type !is UnityPausepointBreakpointType) {
            val action = DumbAwareAction.create(UnityPausepointConstants.convertToPausepointText) {
                val dataContext = it.dataContext
                val editor = CommonDataKeys.EDITOR.getData(dataContext) ?: return@create
                val project = editor.project ?: return@create

                @Suppress("UNCHECKED_CAST")
                convertToPausepoint(project, breakpoint as XLineBreakpoint<DotNetLineBreakpointProperties>, editor)
            }
            return listOf<AnAction>(action)
        }
        return emptyList()
    }
}
