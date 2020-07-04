package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.attach.XLocalAttachDebugger
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityProcess

class UnityLocalAttachDebugger(private val unityProcessInfo: UnityProcessInfo?) : XLocalAttachDebugger {
    override fun getDebuggerDisplayName() = "Unity debugger"

    override fun attachDebugSession(project: Project, processInfo: ProcessInfo) {
        val process: UnityProcess = when {
            unityProcessInfo?.roleName != null -> {
                UnityEditorHelper(processInfo.executableName, unityProcessInfo.roleName, processInfo.pid, unityProcessInfo.projectName)
            }
            else -> {
                // It must be an editor. If it was an editor helper, we'd have a role name
                UnityEditor(processInfo.executableName, processInfo.pid, unityProcessInfo?.projectName)
            }
        }

        // Note that "Attach to Process" doesn't add to the user's list of run configurations
        attachToUnityProcess(project, process)
    }
}