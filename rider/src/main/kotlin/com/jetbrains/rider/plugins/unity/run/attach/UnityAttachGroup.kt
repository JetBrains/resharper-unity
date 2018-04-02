package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.icons.AllIcons
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.XLocalAttachGroup
import javax.swing.Icon

object UnityAttachGroup : XLocalAttachGroup {
    override fun getOrder(): Int = 3

    override fun getGroupName(): String = "Unity processes"

    override fun getProcessIcon(project: Project, info: ProcessInfo, dataHolder: UserDataHolder): Icon = AllIcons.RunConfigurations.Application

    override fun getProcessDisplayText(project: Project, info: ProcessInfo, dataHolder: UserDataHolder): String {
        return info.executableDisplayName
    }

    override fun compare(project: Project, a: ProcessInfo, b: ProcessInfo, dataHolder: UserDataHolder): Int {
        return a.pid.compareTo(b.pid)
    }
}