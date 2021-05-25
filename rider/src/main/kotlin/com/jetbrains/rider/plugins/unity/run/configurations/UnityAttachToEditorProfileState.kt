package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.platform.util.withLongBackgroundContext
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeDebugProfileState
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.WorkerRunInfo

/**
 * [RunProfileState] to attach to the current Unity editor, optionally entering play mode.
 *
 * This will use the passed [UnityExeDebugProfileState] to start the Editor if it's not already running.
 */
class UnityAttachToEditorProfileState(private val exeDebugProfileState : UnityExeDebugProfileState,
                                      private val remoteConfiguration: UnityAttachToEditorRunConfiguration,
                                      executionEnvironment: ExecutionEnvironment)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, "Unity Editor", true) {

    private val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)
    private val project = executionEnvironment.project

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        // Make sure we have a pid and port to connect. This requires fetching the OS process list, which must happen
        // on the background thread
        return withLongBackgroundContext(lifetime) {
            if (!remoteConfiguration.updatePidAndPort()) {
                logger.trace("Have not found Unity, would start a new Unity Editor instead.")
                exeDebugProfileState.createWorkerRunInfo(lifetime, helper, port)
            }
            else {
                super.createWorkerRunInfo(lifetime, helper, port)
            }
        }
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
                            var prevState = false
                            lt.bracket(opening = {
                                // pass value to backend, which will push Unity to enter play mode.
                                prevState = executionEnvironment.project.solution.frontendBackendModel.playControls.play.valueOrDefault(false)
                                executionEnvironment.project.solution.frontendBackendModel.playControls.play.set(true)
                            }, terminationAction = {
                                // if termination happens before the protocol connection is made, the value is lost
                                // we want to wait for the connection, wait for the value from Unity and only then set our value
                                val project = executionEnvironment.project
                                val model = project.solution.frontendBackendModel
                                model.playControlsInitialized.adviseUntil(project.lifetime){ initialized ->
                                    if (!initialized)
                                        return@adviseUntil false
                                    model.playControls.play.set(prevState)
                                    return@adviseUntil true
                                }
                            })
                        }
                    }
                }
            }
        }

        // We couldn't find a running instance, start a new one
        if (remoteConfiguration.pid == null) {
            return exeDebugProfileState.execute(executor, runner, workerProcessHandler, lifetime)
        }

        return super.execute(executor, runner, workerProcessHandler)
    }
}