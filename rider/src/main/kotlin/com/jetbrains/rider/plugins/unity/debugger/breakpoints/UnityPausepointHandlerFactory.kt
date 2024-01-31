package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XBreakpoint
import com.intellij.xdebugger.breakpoints.XBreakpointHandler
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.debugger.DotNetBreakpointsManager
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.IDotNetSupportedBreakpointHandlerFactory
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityPausepointAdditionalAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachProfileState
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
import com.jetbrains.rider.projectView.solution

class UnityPausepointHandler(private val debugProcess: DotNetDebugProcess) : XBreakpointHandler<XLineBreakpoint<DotNetLineBreakpointProperties>>(
    UnityPausepointBreakpointType::class.java) {

    private val unityModel = debugProcess.project.solution.frontendBackendModel
    private val registeredBreakpoints = mutableSetOf<XBreakpoint<*>>()

    init {
        if (isSupportedSession()) {
            advisePlayModeChanges()
        }
    }

    override fun registerBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>) {

        // Pausepoints can only be supported if we're debugging the Unity editor. We'll check this by looking at the run
        // configuration - we explicitly only support "Attach to Unity editor" or "Attach to Unity Process". We do not
        // support arbitrary Mono debugging or entering an IP address + port in the "Attach to Unity Process" dialog.
        // These *might* be an editor process, but we don't know for sure
        if (!isSupportedSession()) {
            markBreakpointDisabledInSession(breakpoint, UnityPausepointConstants.unsupportedDebugTargetMessage)
            return
        }

        // If we can guarantee that we're not in play mode, don't register the breakpoint, but mark it as unavailable.
        // We'll register all pausepoints when play mode changes. If we're not connected, we can't tell, so register it.
        if (debugProcess.project.isConnectedToEditor() && !isInPlayMode()) {
            markBreakpointDisabledInSession(breakpoint, UnityPausepointConstants.unavailableWhenNotPlayingMessage)
        }
        else {
            doRegisterBreakpoint(breakpoint)
        }
    }

    override fun unregisterBreakpoint(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>, temporary: Boolean) {
        if (isSupportedSession()) {
            doUnregisterBreakpoint(breakpoint)
        }
    }

    private fun advisePlayModeChanges() {
        // Advise the changes, not the value. We don't want to do anything on the current value
        unityModel.playControls.play.change.advise(debugProcess.sessionLifetime) { playMode ->
            XDebuggerManager.getInstance(debugProcess.project).breakpointManager.getBreakpoints(
                UnityPausepointBreakpointType::class.java).forEach { breakpoint ->
                // We're obviously connected
                if (playMode) {
                    if (breakpoint.isEnabled) {
                        doRegisterBreakpoint(breakpoint)
                    }
                }
                else {
                    doUnregisterBreakpoint(breakpoint)
                    markBreakpointDisabledInSession(breakpoint, UnityPausepointConstants.unavailableWhenNotPlayingMessage)
                }
            }
        }
    }

    private fun isSupportedSession(): Boolean {
        val runProfile = debugProcess.session.runProfile
        return runProfile is UnityAttachToEditorRunConfiguration
               || (runProfile is UnityAttachProfileState && runProfile.isEditor)
    }

    private fun isInPlayMode(): Boolean {
        return unityModel.playControls.play.valueOrDefault(false)
    }

    private fun doRegisterBreakpoint(breakpoint: XLineBreakpoint<*>) {
        if (!registeredBreakpoints.contains(breakpoint)) {
            var userData = breakpoint.getUserData(DotNetBreakpointsManager.AdditionalActionsKey)
            if (userData == null)
                userData = ArrayList()

            userData.add(UnityPausepointAdditionalAction())
            breakpoint.putUserData(DotNetBreakpointsManager.AdditionalActionsKey, userData)
            // Not safe to call multiple times, make sure we only register each breakpoint once
            debugProcess.breakpointsManager.registerLineBreakpoint(breakpoint)
            registeredBreakpoints.add(breakpoint)
        }
    }

    private fun doUnregisterBreakpoint(breakpoint: XLineBreakpoint<*>) {
        debugProcess.breakpointsManager.unregisterLineBreakpoint(breakpoint)
        registeredBreakpoints.remove(breakpoint)
    }

    private fun markBreakpointDisabledInSession(breakpoint: XLineBreakpoint<*>, message: String) {
        debugProcess.session.updateBreakpointPresentation(breakpoint, breakpoint.type.mutedDisabledIcon, message)
    }
}

class UnityPausepointHandlerFactory : IDotNetSupportedBreakpointHandlerFactory {
    override fun createHandler(debugProcess: DotNetDebugProcess): XBreakpointHandler<*> {
        return UnityPausepointHandler(debugProcess)
    }
}