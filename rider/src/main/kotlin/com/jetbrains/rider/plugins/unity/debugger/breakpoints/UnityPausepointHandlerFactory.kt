package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.execution.configurations.RunProfile
import com.intellij.xdebugger.breakpoints.XBreakpointHandler
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.IDotNetSupportedBreakpointHandlerFactory
import com.jetbrains.rider.plugins.unity.run.attach.UnityAttachProcessConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration

class UnityPausepointHandler(private val debugProcess: DotNetDebugProcess) : XBreakpointHandler<XLineBreakpoint<DotNetLineBreakpointProperties>>(UnityPausepointBreakpointType::class.java) {
    override fun registerBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {

        // Pausepoints can only be supported if we're debugging the Unity editor. We'll check this by looking at the run
        // configuration - we explicitly only support "Attach to Unity editor" or "Attach to Unity Process". We do not
        // support arbitrary Mono debugging or entering an IP address + port in the "Attach to Unity Process" dialog.
        // These *might* be an editor process, but we don't know for sure
        if (isAttachToEditorRunConfiguration(debugProcess.session.runProfile)) {
            debugProcess.breakpointsManager.registerLineBreakpoint(breakpoint)
        }
        else {
            debugProcess.session.updateBreakpointPresentation(breakpoint, breakpoint.type.mutedDisabledIcon,
                UnityPausepointConstants.unsupportedDebugTargetMessage)
        }
    }

    override fun unregisterBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>, p1: Boolean) {
        if (isAttachToEditorRunConfiguration(debugProcess.session.runProfile)) {
            debugProcess.breakpointsManager.unregisterLineBreakpoint(breakpoint)
        }
    }

    private fun isAttachToEditorRunConfiguration(runProfile: RunProfile?): Boolean {
        return runProfile is UnityAttachToEditorRunConfiguration
            || (runProfile is UnityAttachProcessConfiguration && runProfile.isEditor)
    }
}

class UnityPausepointHandlerFactory : IDotNetSupportedBreakpointHandlerFactory {
    override fun createHandler(debugProcess: DotNetDebugProcess): XBreakpointHandler<*> {
        return UnityPausepointHandler(debugProcess)
    }
}