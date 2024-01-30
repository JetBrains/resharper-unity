package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityLocalUwpStartInfo
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

class UnityAttachLocalUwpProfileState(private val remoteConfiguration: RemoteConfiguration,
                                      executionEnvironment: ExecutionEnvironment,
                                      targetName: String,
                                      private val packageName: String)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, targetName, false) {

    override suspend fun createModelStartInfo(lifetime: Lifetime): DebuggerStartInfoBase {
        return UnityLocalUwpStartInfo(
            packageName,
            remoteConfiguration.address,
            remoteConfiguration.port,
            false,
            getUnityBundlesList(),
            getUnityPackagesList(executionEnvironment.project)
        )
    }
}