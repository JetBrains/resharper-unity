package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.UserDataHolder
import com.intellij.openapi.util.getOrCreateUserDataUnsafe
import com.intellij.xdebugger.attach.*
import com.jetbrains.rider.debugger.attach.mono.MonoDebuggersProvider
import com.jetbrains.rider.plugins.unity.run.UnityLocalProcessExtraDetails
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil

class UnityLocalAttachProcessDebuggerProvider : XAttachDebuggerProvider, MonoDebuggersProvider {

    companion object {
        val PROCESS_INFO_KEY: Key<MutableMap<Int, UnityLocalProcessExtraDetails>> = Key("UnityProcess::Info")
    }

    override fun getAvailableDebuggers(project: Project,
                                       host: XAttachHost,
                                       process: ProcessInfo,
                                       userData: UserDataHolder): MutableList<XAttachDebugger> {
        if (UnityRunUtil.isUnityEditorProcess(process)) {

            // Fetch the project + role names while we're not on the EDT, and cache so we can use it in the presenter
            val unityProcessInfo = UnityRunUtil.getUnityProcessInfo(process, project)?.apply {
              val map = userData.getOrCreateUserDataUnsafe(PROCESS_INFO_KEY) { mutableMapOf() }
              map[process.pid] = this
            }
            return mutableListOf(UnityLocalAttachDebugger(unityProcessInfo))
        }
        return mutableListOf()
    }

    override fun isAttachHostApplicable(host: XAttachHost) = host is LocalAttachHost
    override fun getPresentationGroup(): XAttachPresentationGroup<ProcessInfo> = UnityLocalAttachProcessPresentationGroup
}