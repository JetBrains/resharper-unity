package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.attach.XLocalAttachDebugger
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil

class UnityLocalAttachDebugger : XLocalAttachDebugger {
    override fun getDebuggerDisplayName() = "Unity debugger"

    override fun attachDebugSession(project: Project, processInfo: ProcessInfo) {
        // We can safely assume that since it's a local process, it's the editor (standalone players are announced via UDP)
        UnityRunUtil.attachToLocalUnityProcess(processInfo.pid, project)
    }
}