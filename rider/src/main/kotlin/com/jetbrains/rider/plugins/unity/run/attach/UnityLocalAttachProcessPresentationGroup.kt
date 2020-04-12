package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.XAttachProcessPresentationGroup
import icons.UnityIcons
import javax.swing.Icon

@Suppress("UnstableApiUsage")
object UnityLocalAttachProcessPresentationGroup : XAttachProcessPresentationGroup {
    override fun getOrder(): Int = 3
    override fun getGroupName(): String = "Local Unity processes"
    override fun getItemIcon(project: Project, process: ProcessInfo, userData: UserDataHolder) = UnityIcons.Icons.UnityLogo

    override fun getItemDisplayText(project: Project, process: ProcessInfo, userData: UserDataHolder): String {
        val displayNames = userData.getUserData(UnityLocalAttachProcessDebuggerProvider.PROCESS_INFO_KEY)?.get(process.pid)
        val projectName = if (displayNames?.projectName != null) " (${displayNames.projectName})" else ""
        val roleName = if (displayNames?.roleName != null) " ${displayNames.roleName}" else ""
        return process.executableDisplayName + roleName + projectName
    }

    override fun compare(p1: ProcessInfo, p2: ProcessInfo) = p1.pid.compareTo(p2.pid)

    // Scheduled for removal 2020.1
    override fun getProcessDisplayText(project: Project, processInfo: ProcessInfo, userDataHolder: UserDataHolder): String {
        return getItemDisplayText(project, processInfo, userDataHolder)
    }

    override fun getProcessIcon(project: Project, processInfo: ProcessInfo, userDataHolder: UserDataHolder): Icon {
        return getItemIcon(project, processInfo, userDataHolder)
    }
}