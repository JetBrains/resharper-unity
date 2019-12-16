package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.UserDataHolder
import com.intellij.xdebugger.attach.*
import com.jetbrains.rdclient.util.idea.getOrCreateUserData
import com.jetbrains.rider.plugins.unity.run.UnityProcessInfo
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil

@Suppress("UnstableApiUsage")
class UnityLocalAttachProcessDebuggerProvider : XAttachDebuggerProvider {

    companion object {
        val PROCESS_INFO_KEY: Key<MutableMap<Int, UnityProcessInfo>> = Key("UnityProcess::Info")
    }

    override fun getAvailableDebuggers(project: Project, host: XAttachHost, process: ProcessInfo, userData: UserDataHolder): MutableList<XAttachDebugger> {
        if (UnityRunUtil.isUnityEditorProcess(process)) {
            // Cache the processes display names. When we're asked for the display text for the menu, we're on the EDT
            // thread, and can't call this
            UnityRunUtil.getUnityProcessInfo(process, project)?.let {
                val map = userData.getOrCreateUserData(PROCESS_INFO_KEY) { mutableMapOf() }
                map[process.pid]= it
            }
            return mutableListOf(UnityLocalAttachDebugger())
        }
        return mutableListOf()
    }

    override fun isAttachHostApplicable(host: XAttachHost) = host is LocalAttachHost
    override fun getPresentationGroup(): XAttachPresentationGroup<ProcessInfo> = UnityLocalAttachProcessPresentationGroup
}