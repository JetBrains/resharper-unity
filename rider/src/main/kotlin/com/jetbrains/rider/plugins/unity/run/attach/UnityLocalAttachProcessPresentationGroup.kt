package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.XAttachProcessPresentationGroup
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object UnityLocalAttachProcessPresentationGroup : XAttachProcessPresentationGroup {
    override fun getOrder(): Int = 3
    override fun getGroupName(): String = UnityBundle.message("group.name.local.unity.processes")
    override fun getItemIcon(project: Project, process: ProcessInfo, userData: UserDataHolder) = UnityIcons.Icons.UnityLogo

    override fun getItemDisplayText(project: Project, process: ProcessInfo, userData: UserDataHolder): String {
        val displayNames = userData.getUserData(UnityLocalAttachProcessDebuggerProvider.PROCESS_INFO_KEY)?.get(process.pid)

        @NlsSafe
        val projectName = if (displayNames?.projectName != null) " (${displayNames.projectName})" else ""
        val roleName = if (displayNames?.instanceName != null) " ${displayNames.instanceName}" else ""
        return process.executableDisplayName + roleName + projectName
    }

    override fun compare(p1: ProcessInfo, p2: ProcessInfo) = p1.pid.compareTo(p2.pid)
}