package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.attach.XLocalAttachDebugger
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityProcess

class UnityLocalAttachDebugger(private val unityProcessInfo: UnityLocalProcessExtraDetails?) : XLocalAttachDebugger {
    override fun getDebuggerDisplayName() = UnityBundle.message("unity.debugger")

    override fun attachDebugSession(project: Project, processInfo: ProcessInfo) {
        val process = processInfo.toUnityProcess(unityProcessInfo)
        attachToUnityProcess(project, process)
    }
}