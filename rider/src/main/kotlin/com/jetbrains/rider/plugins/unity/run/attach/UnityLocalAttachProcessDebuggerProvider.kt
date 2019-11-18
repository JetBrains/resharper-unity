package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.*
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil

@Suppress("UnstableApiUsage")
class UnityLocalAttachProcessDebuggerProvider : XAttachDebuggerProvider {

    companion object {
        val PROJECT_NAME_KEY: Key<String> = Key("UnityProcess::ProjectName")
    }

    override fun getAvailableDebuggers(project: Project, host: XAttachHost, process: ProcessInfo, userData: UserDataHolder): MutableList<XAttachDebugger> {
        if (UnityRunUtil.isUnityEditorProcess(process)) {
            // Cache the project name. When we're asked for display name, we're on the EDT thread, and can't call this
            UnityRunUtil.getUnityProcessProjectName(process, project)?.let {
                userData.putUserData(PROJECT_NAME_KEY, it)
            }
            return mutableListOf(UnityLocalAttachDebugger())
        }
        return mutableListOf()
    }

    override fun isAttachHostApplicable(host: XAttachHost) = host is LocalAttachHost
    override fun getPresentationGroup(): XAttachPresentationGroup<ProcessInfo> = UnityLocalAttachProcessPresentationGroup
}