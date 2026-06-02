package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.model.debuggerWorker.DebuggerWorkerModel
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityLocalCoreClrStartInfo
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityMonoStartInfo
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.unityDebuggerWorkerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.UnityDebugEngine
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.AttachDebugProfileStateBase
import com.jetbrains.rider.run.ConsoleKind
import com.jetbrains.rider.run.IDebuggerOutputListener

/**
 * Simple [RunProfileState] to connect to an already running Unity process via the Mono debugger protocol
 *
 * Use as a base class to correctly handle passing data to the debugger worker and setting up debugger listener for
 * Unity specific warnings
 *
 * @param targetName    Used in user facing "Unable to connect to {targetName}" error message
 */
open class UnityAttachProfileState(private val debugEngine: UnityDebugEngine,
                                   executionEnvironment: ExecutionEnvironment,
                                   private val targetName: String,
                                   val isEditor: Boolean = false)
    : AttachDebugProfileStateBase(executionEnvironment) {

    override val attached: Boolean = true
    override val consoleKind: ConsoleKind = ConsoleKind.AttachedProcess

    protected open var monoListenForConnections: Boolean = false

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        val host = if (debugEngine is UnityDebugEngine.Mono) debugEngine.host else null
        return UnityDebuggerOutputListener(executionEnvironment.project, host, targetName, isEditor)
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
        return when (debugEngine) {
            is UnityDebugEngine.CoreClr -> createCoreClrModelStartInfo(lifetime, debugEngine)
            is UnityDebugEngine.Mono -> createMonoModelStartInfo(lifetime, debugEngine)
        }
    }

    protected open suspend fun createMonoModelStartInfo(lifetime: Lifetime, monoDebugEngine: UnityDebugEngine.Mono): DebuggerStartInfoBase {
        return UnityMonoStartInfo(
            monoDebugEngine.host,
            monoDebugEngine.port,
            monoListenForConnections,
            getUnityBundlesList(),
            getUnityPackagesList(executionEnvironment.project)
        )
    }

    protected open suspend fun createCoreClrModelStartInfo(lifetime: Lifetime, coreClrDebugEngine: UnityDebugEngine.CoreClr): DebuggerStartInfoBase {
        return UnityLocalCoreClrStartInfo(
            coreClrDebugEngine.processId,
            getUnityBundlesList(),
            getUnityPackagesList(executionEnvironment.project)
        )
    }
}
