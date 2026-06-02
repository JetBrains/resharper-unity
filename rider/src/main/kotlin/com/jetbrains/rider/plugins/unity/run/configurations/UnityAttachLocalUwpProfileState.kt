package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityLocalUwpStartInfo
import com.jetbrains.rider.plugins.unity.run.UnityDebugEngine

class UnityAttachLocalUwpProfileState(debugEngine: UnityDebugEngine,
                                      executionEnvironment: ExecutionEnvironment,
                                      targetName: String,
                                      private val packageName: String)
    : UnityAttachProfileState(debugEngine, executionEnvironment, targetName, false) {

    override suspend fun createMonoModelStartInfo(lifetime: Lifetime, monoDebugEngine: UnityDebugEngine.Mono): DebuggerStartInfoBase {
        return UnityLocalUwpStartInfo(
            packageName,
            monoDebugEngine.host,
            monoDebugEngine.port,
            false,
            getUnityBundlesList(),
            getUnityPackagesList(executionEnvironment.project)
        )
    }
}
