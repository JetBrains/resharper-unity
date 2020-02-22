package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.xdebugger.breakpoints.XBreakpointHandler
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.IDotNetSupportedBreakpointHandlerFactory

class UnityPausepointHandler(private val debugProcess: DotNetDebugProcess) : XBreakpointHandler<XLineBreakpoint<DotNetLineBreakpointProperties>>(UnityPausepointBreakpointType::class.java) {
    override fun registerBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {
        debugProcess.breakpointsManager.registerLineBreakpoint(breakpoint)
    }

    override fun unregisterBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>, p1: Boolean) {
        debugProcess.breakpointsManager.registerLineBreakpoint(breakpoint)
    }
}

class UnityPausepointHandlerFactory : IDotNetSupportedBreakpointHandlerFactory {
    override fun createHandler(debugProcess: DotNetDebugProcess): XBreakpointHandler<*> {
        return UnityPausepointHandler(debugProcess)
    }
}