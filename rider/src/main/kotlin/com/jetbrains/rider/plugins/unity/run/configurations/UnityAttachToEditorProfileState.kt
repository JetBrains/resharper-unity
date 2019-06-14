package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.util.ui.UIUtil
import com.intellij.xdebugger.XDebugProcess
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerManagerListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.plugins.unity.util.addPlayModeArguments
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.idea.application
import org.jetbrains.concurrency.AsyncPromise
import org.jetbrains.concurrency.Promise

class UnityAttachToEditorProfileState(private val remoteConfiguration: UnityAttachToEditorRunConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    private val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)
    private val project = executionEnvironment.project

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        if (remoteConfiguration.play) {
            val lt = lifetime.createNested().lifetime
            project.messageBus.connect(lifetime.createNestedDisposable()).subscribe(XDebuggerManager.TOPIC, object : XDebuggerManagerListener {
                override fun processStarted(debugProcess: XDebugProcess) {
                    if (debugProcess is DotNetDebugProcess)
                    {
                        debugProcess.debuggerInitializingState.advise(lt){
                            if (it == DebuggerInitializingState.Initialized)
                            {
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
            })
        }

        return super.execute(executor, runner, workerProcessHandler)
    }

    override fun createWorkerRunCmd(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): Promise<GeneralCommandLine> {
        if (remoteConfiguration.pid != null)
            return super.createWorkerRunCmd(lifetime, helper, port)

        val result = AsyncPromise<GeneralCommandLine>()
        application.executeOnPooledThread {
            try {
                val args = getUnityWithProjectArgs(project)
                if (remoteConfiguration.play) {
                    addPlayModeArguments(args)
                }

                val process = ProcessBuilder(args).start()
                val actualPid = OSProcessUtil.getProcessID(process)
                remoteConfiguration.pid = actualPid
                remoteConfiguration.port = convertPidToDebuggerPort(actualPid)

                Thread.sleep(2000)
                UIUtil.invokeLaterIfNeeded {
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