@file:OptIn(ExperimentalCoroutinesApi::class)

package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.thisLogger
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeDebugProfileState
import com.jetbrains.rider.plugins.unity.ui.hasTrueValue
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.dotNetCore.DotNetCoreDebugProfile
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.withContext

/**
 * [RunProfileState] to attach to the current Unity editor, optionally entering play mode.
 *
 * This will use the passed [UnityExeDebugProfileState] to start the Editor if it's not already running.
 */
class UnityAttachToEditorProfileState(
    private val exeDebugProfileState: UnityExeDebugProfileState,
    private val remoteConfiguration: UnityAttachToEditorRunConfiguration,
    executionEnvironment: ExecutionEnvironment
)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, "Unity Editor", true) {

    private val project = executionEnvironment.project

    private lateinit var corAttachDebugProfileState : UnityCorAttachDebugProfileState
    private lateinit var corRunDebugProfileState: DotNetCoreDebugProfile

    override suspend fun createModelStartInfo(lifetime: Lifetime): DebuggerStartInfoBase {
        if (::corAttachDebugProfileState.isInitialized)
            return corAttachDebugProfileState.createModelStartInfo(lifetime)
        else if (::corRunDebugProfileState.isInitialized)
            return corRunDebugProfileState.createModelStartInfo(lifetime)
        return super.createModelStartInfo(lifetime)
    }

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        // Make sure we have a pid and port to connect. This requires fetching the OS process list, which must happen
        // on the background thread
        return withContext(Dispatchers.Default) {
            if (!remoteConfiguration.updatePidAndPort()) {
                thisLogger().trace("Have not found Unity, would start a new Unity Editor instead.")

                if (UnityInstallationFinder.getInstance(project).isCoreCLR.hasTrueValue()){
                    corRunDebugProfileState = exeDebugProfileState.exeConfiguration.getDotNetCoreDebugProfile(executionEnvironment)
                    corRunDebugProfileState.createWorkerRunInfo(lifetime, helper, port)
                }
                else
                    exeDebugProfileState.createWorkerRunInfo(lifetime, helper, port)
            }
            else if (remoteConfiguration.runtimes.any {it is com.jetbrains.rider.model.GenericCoreClrRuntime }) {
                // at the moment runtimes show both GenericCoreClrRuntime and Mono for Unity 7
                corAttachDebugProfileState = UnityCorAttachDebugProfileState(remoteConfiguration.pid!!,
                                                                             exeDebugProfileState.executionEnvironment)
                corAttachDebugProfileState.createWorkerRunInfo(lifetime, helper, port)
            }
            else {
                // base class serializes listenPortForConnections, so
                // user start debug, Unity is started, user stops debugging, listenPortForConnections is serialized to true
                // later user starts Unity and wants to attach debugger - we need to set listenPortForConnections = false,
                // otherwise old serialized value would be used.
                remoteConfiguration.listenPortForConnections = false
                executionEnvironment.putUserData(DebuggerWorkerProcessHandler.PID_KEY,
                                                 requireNotNull(remoteConfiguration.pid) { "Pid should be initialized in updatePidAndPort" })
                super.createWorkerRunInfo(lifetime, helper, port)
            }
        }
    }

    override suspend fun execute(executor: Executor,
                         runner: ProgramRunner<*>,
                         workerProcessHandler: DebuggerWorkerProcessHandler,
                         lifetime: Lifetime): ExecutionResult {
        if (remoteConfiguration.play) {
            val lt = lifetime.createNested().lifetime
            val processTracker = RiderDebugActiveDotNetSessionsTracker.getInstance(project)
            processTracker.dotNetDebugProcesses.change.advise(lifetime) { (event, debugProcess) ->
                if (event == AddRemove.Add) {
                    debugProcess.initializeDebuggerTask.debuggerInitializingState.advise(lt) {
                        if (it == DebuggerInitializingState.Initialized) {
                            thisLogger().info("Pass value to backend, which will push Unity to enter play mode.")
                            var prevState = false
                            lt.bracketIfAlive(opening = {
                                // pass value to backend, which will push Unity to enter play mode.
                                prevState = executionEnvironment.project.solution.frontendBackendModel.playControls.play.valueOrDefault(
                                    false)
                                executionEnvironment.project.solution.frontendBackendModel.playControls.play.set(true)
                            }, terminationAction = {
                                // if termination happens before the protocol connection is made, the value is lost
                                // we want to wait for the connection, wait for the value from Unity and only then set our value
                                val project = executionEnvironment.project
                                val model = project.solution.frontendBackendModel
                                model.playControlsInitialized.adviseUntil(UnityProjectLifetimeService.getLifetime(project)) { initialized ->
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
            if (::corRunDebugProfileState.isInitialized)
                return corRunDebugProfileState.execute(executor, runner, workerProcessHandler, lifetime)
            return exeDebugProfileState.execute(executor, runner, workerProcessHandler, lifetime)
        }

        if (::corAttachDebugProfileState.isInitialized)
            return corAttachDebugProfileState.execute(executor, runner, workerProcessHandler, lifetime)

        return super.execute(executor, runner, workerProcessHandler)
    }
}