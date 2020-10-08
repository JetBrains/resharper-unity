package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RuntimeConfigurationError
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.addPlayModeArguments
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.startLongBackgroundAsync
import kotlinx.coroutines.delay

class UnityAttachToEditorProfileState(private val remoteConfiguration: UnityAttachToEditorRunConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    private val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)
    private val project = executionEnvironment.project

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

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        // alternative approach
        // it is better because mono waits for attach, so rider would not miss any frames,
        // but if you disconnect and try to attach again it fails, because updatePidAndPort would try different port
        // val actualPort = com.jetbrains.rider.util.NetUtils.findFreePort(500013, setOf(port))
        // val processBuilder = ProcessBuilder(args)
        // processBuilder.environment().set("MONO_ARGUMENTS", "--debugger-agent=transport=dt_socket,address=${remoteConfiguration.address}:$actualPort,embedding=1,server=y,suspend=y")
        // val process = processBuilder.start()
        // val actualPid = OSProcessUtil.getProcessID(process)
        // remoteConfiguration.pid = actualPid
        // remoteConfiguration.port = actualPort

        lifetime.startLongBackgroundAsync {
            if (!remoteConfiguration.updatePidAndPort()) {
                logger.trace("Do not found Unity, starting new Unity Editor")

                val model = project.solution.rdUnityModel
                if (UnityInstallationFinder.getInstance(project).getApplicationExecutablePath() == null ||
                    model.hasUnityReference.hasTrueValue && !project.isUnityProject()) {
                    throw RuntimeConfigurationError("Cannot automatically determine Unity Editor instance. Please open the project in Unity and try again.")
                }

                val args = getUnityWithProjectArgs(project)
                if (remoteConfiguration.play) {
                    addPlayModeArguments(args)
                }

                val process = ProcessBuilder(args).start()
                val actualPid = OSProcessUtil.getProcessID(process)
                remoteConfiguration.pid = actualPid
                remoteConfiguration.port = convertPidToDebuggerPort(actualPid)

                delay(2000)
            }
        }.await()

        logger.trace("DebuggerWorker port: $port")
        logger.trace("Connecting to Unity Editor with port: ${remoteConfiguration.port}")
        val cmd = super.createWorkerRunInfo(lifetime, helper, port)
        cmd.commandLine.withUnityExtensionsEnabledEnvironment(project)
        return cmd
    }

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(project, remoteConfiguration.address, "Unity Editor", true)
    }
}