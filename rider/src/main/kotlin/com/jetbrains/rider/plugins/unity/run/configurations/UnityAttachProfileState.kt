package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

/**
 * Simple [RunProfileState] to connect to an already running Unity process via the Mono debugger protocol
 *
 * Use as a base class to correctly handle passing data to the debugger worker and setting up debugger listener for
 * Unity specific warnings
 */
open class UnityAttachProfileState(private val remoteConfiguration: RemoteConfiguration,
                                   executionEnvironment: ExecutionEnvironment,
                                   private val targetName: String,
                                   val isEditor: Boolean)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        val runCmd = super.createWorkerRunInfo(lifetime, helper, port)
        runCmd.commandLine.withUnityExtensionsEnabledEnvironment(executionEnvironment.project)
        return runCmd
    }

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(
            executionEnvironment.project,
            remoteConfiguration.address,
            targetName,
            isEditor
        )
    }
}