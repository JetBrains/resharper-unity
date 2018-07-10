package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.attach.XLocalAttachDebugger

class UnityAttachDebugger : XLocalAttachDebugger {
    override fun getDebuggerDisplayName() = "Unity debugger"

    override fun attachDebugSession(project: Project, processInfo: ProcessInfo) {
        UnityRunUtil.runAttach(processInfo.pid, project)
    }
}