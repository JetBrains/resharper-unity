package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.XAttachProcessPresentationGroup
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import icons.UnityIcons

@Suppress("UnstableApiUsage")
object UnityLocalAttachProcessPresentationGroup : XAttachProcessPresentationGroup {
    override fun getOrder(): Int = 3
    override fun getGroupName(): String = "Local Unity processes"
    override fun getItemIcon(project: Project, process: ProcessInfo, userData: UserDataHolder) = UnityIcons.Icons.UnityLogo

    override fun getItemDisplayText(project: Project, process: ProcessInfo, userData: UserDataHolder): String {
        val projectName = UnityRunUtil.getUnityProcessProjectName(process, project)
        return if (projectName != null) {
            "${process.executableDisplayName} ($projectName)"
        }
        else {
            process.executableDisplayName
        }
    }

    override fun compare(p1: ProcessInfo, p2: ProcessInfo) = p1.pid.compareTo(p2.pid)
}