package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.xdebugger.breakpoints.SuspendPolicy
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import icons.UnityIcons
import java.util.*
import javax.swing.Icon

class UnityPausepointBreakpointType : DotNetLineBreakpointType(Id, Title) {
    companion object {
        const val Id = "UnityPausepointType"
        const val Title = "Unity Pausepoints"
    }

    override fun getDisplayText(breakpoint: XLineBreakpoint<DotNetLineBreakpointProperties>?): String {
        return "Pause Unity editor when debugger reaches " + super.getDisplayText(breakpoint)
    }

    override fun getDisabledIcon(): Icon = UnityIcons.Debugger.Db_disabled_pausepoint
    override fun getEnabledIcon(): Icon = UnityIcons.Debugger.Db_set_pausepoint
    override fun getInactiveDependentIcon(): Icon = UnityIcons.Debugger.Db_dep_line_pausepoint
    override fun getInvalidIcon(): Icon = UnityIcons.Debugger.Db_invalid_pausepoint
    override fun getMutedDisabledIcon(): Icon = UnityIcons.Debugger.Db_muted_disabled_pausepoint
    override fun getMutedEnabledIcon(): Icon = UnityIcons.Debugger.Db_muted_pausepoint
    override fun getSuspendNoneIcon(): Icon = UnityIcons.Debugger.Db_no_suspend_pausepoint
    // Temporary is remove-once-hit
    override fun getTemporaryIcon(): Icon = UnityIcons.Debugger.Db_set_pausepoint
    override fun getVerifiedIcon() = UnityIcons.Debugger.Db_verified_pausepoint
    override fun getVerifiedIconWithNoSuspend() = UnityIcons.Debugger.Db_verified_no_suspend_pausepoint
    // getPendingIcon is not overridden. Base will return null, which then uses set icon

    // Don't allow adding the breakpoint manually
    override fun canPutAt(file: VirtualFile, line: Int, project: Project): Boolean = false

    override fun getPriority(): Int = super.getPriority() - 1
    override fun getDefaultSuspendPolicy() = SuspendPolicy.NONE

    override fun getVisibleStandardPanels(): EnumSet<StandardPanels> = EnumSet.of(StandardPanels.DEPENDENCY)
}