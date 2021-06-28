package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.model.debuggerWorker.DebuggerWorkerModel
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityStartInfo
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.unityDebuggerWorkerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.projectView.solution
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
        return super.createWorkerRunInfo(lifetime, helper, port)
    }

    final override suspend fun createDebuggerWorker(
        workerCmd: GeneralCommandLine,
        protocolModel: DebuggerWorkerModel,
        protocolServerPort: Int,
        projectLifetime: Lifetime
    ): DebuggerWorkerProcessHandler {

        val debuggerWorkerLifetime = projectLifetime.createNested()

        val frontendBackendModel = executionEnvironment.project.solution.frontendBackendModel
        frontendBackendModel.backendSettings.enableDebuggerExtensions.flowInto(debuggerWorkerLifetime,
            protocolModel.unityDebuggerWorkerModel.showCustomRenderers)
        frontendBackendModel.backendSettings.ignoreBreakOnUnhandledExceptionsForIl2Cpp.flowInto(debuggerWorkerLifetime,
            protocolModel.unityDebuggerWorkerModel.ignoreBreakOnUnhandledExceptionsForIl2Cpp)

        return super.createDebuggerWorker(workerCmd, protocolModel, protocolServerPort, projectLifetime).apply {
            addProcessListener(object : ProcessAdapter() {
                override fun processTerminated(event: ProcessEvent) { debuggerWorkerLifetime.terminate() }
            })
        }
    }

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(
            executionEnvironment.project,
            remoteConfiguration.address,
            targetName,
            isEditor
        )
    }

    override suspend fun createModelStartInfo(lifetime: Lifetime): DebuggerStartInfoBase {
        return UnityStartInfo(remoteConfiguration.address,
            remoteConfiguration.port,
            remoteConfiguration.listenPortForConnections)
    }
}