package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.model.debuggerWorker.DebuggerWorkerModel
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityStartInfo
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.unityDebuggerWorkerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

/**
 * Simple [RunProfileState] to connect to an already running Unity process via the Mono debugger protocol
 *
 * Use as a base class to correctly handle passing data to the debugger worker and setting up debugger listener for
 * Unity specific warnings
 *
 * @param targetName    Used in user facing "Unable to connect to {targetName}" error message
 */
open class UnityAttachProfileState(private val remoteConfiguration: RemoteConfiguration,
                                   executionEnvironment: ExecutionEnvironment,
                                   private val targetName: String,
                                   val isEditor: Boolean = false)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(
            executionEnvironment.project,
            remoteConfiguration.address,
            targetName,
            isEditor
        )
    }

    override fun bindSettings(lifetime: Lifetime, workerModel: DebuggerWorkerModel) {
        val frontendBackendModel = executionEnvironment.project.solution.frontendBackendModel
        frontendBackendModel.backendSettings.enableDebuggerExtensions.flowInto(lifetime,
            workerModel.unityDebuggerWorkerModel.showCustomRenderers)
        frontendBackendModel.backendSettings.ignoreBreakOnUnhandledExceptionsForIl2Cpp.flowInto(lifetime,
            workerModel.unityDebuggerWorkerModel.ignoreBreakOnUnhandledExceptionsForIl2Cpp)
        frontendBackendModel.backendSettings.forcedTimeoutForAdvanceUnityEvaluation.flowInto(lifetime,
            workerModel.unityDebuggerWorkerModel.forcedTimeoutForAdvanceUnityEvaluation)
        frontendBackendModel.backendSettings.breakpointTraceOutput.flowInto(lifetime,
            workerModel.unityDebuggerWorkerModel.breakpointTraceOutput)
        super.bindSettings(lifetime, workerModel)
    }

    override suspend fun createModelStartInfo(lifetime: Lifetime): DebuggerStartInfoBase {
        return UnityStartInfo(remoteConfiguration.address,
            remoteConfiguration.port,
            remoteConfiguration.listenPortForConnections,
            getUnityBundlesList(),
            getUnityPackagesList(executionEnvironment.project))
    }
}