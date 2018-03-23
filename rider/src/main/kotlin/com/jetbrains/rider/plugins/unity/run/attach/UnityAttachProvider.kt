package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.XLocalAttachDebugger
import com.intellij.xdebugger.attach.XLocalAttachDebuggerProvider
import com.intellij.xdebugger.attach.XLocalAttachGroup

class UnityAttachProvider : XLocalAttachDebuggerProvider {

    override fun getAttachGroup(): XLocalAttachGroup = UnityAttachGroup

    override fun getAvailableDebuggers(project: Project, processInfo: ProcessInfo, contextHolder: UserDataHolder): List<XLocalAttachDebugger> {
        if (UnityProcessUtil.isUnityEditorProcess(processInfo))
            return arrayListOf(UnityAttachDebugger())
        return emptyList()
    }

}