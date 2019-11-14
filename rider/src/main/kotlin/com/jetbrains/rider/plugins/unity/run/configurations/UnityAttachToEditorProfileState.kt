package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RuntimeConfigurationError
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.Logger
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.addPlayModeArguments
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.getComponent
import org.jetbrains.concurrency.AsyncPromise
import org.jetbrains.concurrency.Promise

class UnityAttachToEditorProfileState(private val remoteConfiguration: UnityAttachToEditorRunConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    private val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)
    private val project = executionEnvironment.project

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        if (remoteConfiguration.play) {
            val lt = lifetime.createNested().lifetime
            val processTracker: RiderDebugActiveDotNetSessionsTracker = project.getComponent()
            processTracker.dotNetDebugProcesses.change.advise(lifetime) { (event, debugProcess) ->
                if (event == AddRemove.Add) {
                    debugProcess.initializeDebuggerTask.debuggerInitializingState.advise(lt) {
                        if (it == DebuggerInitializingState.Initialized) {
                            logger.info("Pass value to backend, which will push Unity to enter play mode.")
                            lt.bracket(opening = {
                                // pass value to backend, which will push Unity to enter play mode.
                                executionEnvironment.project.solution.rdUnityModel.play.set(true)
                            }, terminationAction = {
                                executionEnvironment.project.solution.rdUnityModel.play.set(false)
                            })
                        }
                    }
                }
            }
        }

        return super.execute(executor, runner, workerProcessHandler)
    }

    override fun createWorkerRunCmd(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): Promise<WorkerRunInfo> {

        val result = AsyncPromise<WorkerRunInfo>()
        application.executeOnPooledThread {
            try {
                if (!remoteConfiguration.updatePidAndPort()) {
                    logger.trace("Do not found Unity, starting new Unity Editor")
                    val model = UnityHost.getInstance(project).model
                    if (UnityInstallationFinder.getInstance(project).getApplicationPath() == null ||
                        model.hasUnityReference.hasTrueValue && !UnityProjectDiscoverer.getInstance(project).isUnityProjectFolder)
                        throw RuntimeConfigurationError("Cannot automatically determine Unity Editor instance. Please open the project in Unity and try again.")

                    val args = getUnityWithProjectArgs(project)
                    if (remoteConfiguration.play) {
                        addPlayModeArguments(args)
                    }

                    val process = ProcessBuilder(args).start()
                    val actualPid = OSProcessUtil.getProcessID(process)
                    remoteConfiguration.pid = actualPid
                    remoteConfiguration.port = convertPidToDebuggerPort(actualPid)

                    Thread.sleep(2000)
                }
                UIUtil.invokeLaterIfNeeded {
                    logger.trace("Connecting to Unity Editor with port: $port")
                    super.createWorkerRunCmd(lifetime, helper, port).onSuccess { result.setResult(it) }.onError { result.setError(it) }
                }
            }
            catch (e: Exception) {
                result.setError(e)
            }
        }
        return result
    }

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(project, remoteConfiguration.address, "Unity Editor", true)
    }
}