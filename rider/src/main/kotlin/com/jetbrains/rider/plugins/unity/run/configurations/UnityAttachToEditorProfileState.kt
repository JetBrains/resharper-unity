package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rd.platform.util.withLongBackgroundContext
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeDebugProfileState
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.NetUtils

class UnityAttachToEditorProfileState(private val exeDebugProfileState : UnityExeDebugProfileState, private val remoteConfiguration: UnityAttachToEditorRunConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    private val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)
    private val project = executionEnvironment.project

    override fun execute(executor: Executor?, runner: ProgramRunner<*>): ExecutionResult? {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler): ExecutionResult {
        throw UnsupportedOperationException("Should use overload with session")
    }
    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        if (remoteConfiguration.play) {
            val lt = lifetime.createNested().lifetime
            val processTracker = RiderDebugActiveDotNetSessionsTracker.getInstance(project)
            processTracker.dotNetDebugProcesses.change.advise(lifetime) { (event, debugProcess) ->
                if (event == AddRemove.Add) {
                    debugProcess.initializeDebuggerTask.debuggerInitializingState.advise(lt) {
                        if (it == DebuggerInitializingState.Initialized) {
                            logger.info("Pass value to backend, which will push Unity to enter play mode.")
                            lt.bracket(opening = {
                                // pass value to backend, which will push Unity to enter play mode.
                                executionEnvironment.project.solution.frontendBackendModel.playControls.play.set(true)
                            }, terminationAction = {
                                executionEnvironment.project.solution.frontendBackendModel.playControls.play.set(false)
                            })
                        }
                    }
                }
            }
        }

        if (remoteConfiguration.listenPortForConnections)
            return exeDebugProfileState.execute(executor, runner, workerProcessHandler, lifetime)
        return super.execute(executor, runner, workerProcessHandler)
    }

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        withLongBackgroundContext(lifetime) {
            if (!remoteConfiguration.updatePidAndPort()) {
                logger.trace("Have not found Unity, would start a new Unity Editor instead.")

                remoteConfiguration.listenPortForConnections = true
                remoteConfiguration.port = NetUtils.findFreePort(500013, setOf(port))
                remoteConfiguration.address = "127.0.0.1"
            }
        }

        val cmd = super.createWorkerRunInfo(lifetime, helper, port)
        cmd.commandLine.withUnityExtensionsEnabledEnvironment(project)
        return cmd
    }

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(project, remoteConfiguration.address, "Unity Editor", true)
    }
}