package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import icons.UnityIcons
import java.util.*
import javax.swing.Icon

class UnityPausepointBreakpointType : DotNetLineBreakpointType(Id, Title) {
    companion object {
        const val Id = "UnityPausepointType"
        const val Title = "Unity pausepoints"
    }

    override fun getTitle(): String = Title

    override fun getDisabledIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun getEnabledIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun getInactiveDependentIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun getMutedDisabledIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun getMutedEnabledIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun getPendingIcon(): Icon? = UnityIcons.Icons.UnityLogo

    override fun getSuspendNoneIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun getTemporaryIcon(): Icon = UnityIcons.Icons.UnityLogo

    override fun canPutAt(file: VirtualFile, line: Int, project: Project): Boolean = false

    override fun getPriority(): Int = super.getPriority() - 1

    override fun getVisibleStandardPanels(): EnumSet<StandardPanels> = EnumSet.of(StandardPanels.DEPENDENCY)
}