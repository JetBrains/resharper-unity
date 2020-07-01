package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.KillableProcessHandler
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessListener
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.util.Key
import com.jetbrains.rd.util.addUnique
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerWorkerPlatform
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.tryWriteMessageToConsoleView
import com.jetbrains.rider.model.debuggerWorker.OutputMessageWithSubject
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.run.ExternalConsoleMediator
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.createEmptyConsoleCommandLine
import com.jetbrains.rider.run.withRawParameters
import com.jetbrains.rider.util.NetUtils
import com.jetbrains.rider.util.idea.createNestedAsyncPromise
import org.jetbrains.concurrency.Promise

class UnityExeDebugProfileState(private val exeConfiguration : UnityExeConfiguration, private val remoteConfiguration: RemoteConfiguration,
                                executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    override fun execute(executor: Executor?, runner: ProgramRunner<*>): ExecutionResult? {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler): ExecutionResult {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        val envs = exeConfiguration.parameters.envs.toMutableMap()
        envs.addUnique(lifetime, "MONO_ARGUMENTS", "--debugger-agent=transport=dt_socket,address=127.0.0.1:${remoteConfiguration.port},server=n,suspend=y")
        val runCommandLine = createEmptyConsoleCommandLine(exeConfiguration.parameters.useExternalConsole)
            .withEnvironment(envs)
            .withParentEnvironmentType(if (exeConfiguration.parameters.isPassParentEnvs) {
                GeneralCommandLine.ParentEnvironmentType.CONSOLE
            } else {
                GeneralCommandLine.ParentEnvironmentType.NONE
            })
            .withExePath(exeConfiguration.parameters.exePath)
            .withWorkDirectory(exeConfiguration.parameters.workingDirectory)
            .withRawParameters(exeConfiguration.parameters.programParameters)

        val commandLineString = runCommandLine.commandLineString
        val monoConnectResult = super.execute(executor, runner, workerProcessHandler)
        workerProcessHandler.debuggerWorkerRealHandler.addProcessListener(object : ProcessAdapter() {
            override fun startNotified(event: ProcessEvent) {
                val targetProcessHandler = if (exeConfiguration.parameters.useExternalConsole)
                    ExternalConsoleMediator.createProcessHandler(runCommandLine) as KillableProcessHandler
                else
                    KillableProcessHandler(runCommandLine)

                lifetime.onTermination {
                    if (!targetProcessHandler.isProcessTerminated) {
                        targetProcessHandler.killProcess()
                    }
                }

                targetProcessHandler.addProcessListener(object : ProcessListener {
                    override fun onTextAvailable(processEvent: ProcessEvent, key: Key<*>) {
                        monoConnectResult.executionConsole.tryWriteMessageToConsoleView(
                            OutputMessageWithSubject(processEvent.text, OutputType.Info, OutputSubject.Default)
                        )
                    }

                    override fun processTerminated(processEvent: ProcessEvent) {
                        monoConnectResult.executionConsole.tryWriteMessageToConsoleView(OutputMessageWithSubject(output = "Process \"$commandLineString\" terminated with exit code ${processEvent.exitCode}.\r\n", type = OutputType.Warning, subject = OutputSubject.Default))
                    }

                    override fun startNotified(processEvent: ProcessEvent) {
                        monoConnectResult.executionConsole.tryWriteMessageToConsoleView(OutputMessageWithSubject("Process \"$commandLineString\" started.\r\n", OutputType.Info, OutputSubject.Default))
                    }
                })

                targetProcessHandler.startNotify()
                super.startNotified(event)
            }
        })

        return monoConnectResult
    }

    override fun createWorkerRunCmd(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): Promise<WorkerRunInfo> {
        remoteConfiguration.listenPortForConnections = true
        remoteConfiguration.port = NetUtils.findFreePort(500013, setOf(port))
        remoteConfiguration.address = "127.0.0.1"

        val result = lifetime.createNestedAsyncPromise<WorkerRunInfo>()
        result.setResult(createWorkerRunInfoFor(port, DebuggerWorkerPlatform.AnyCpu))
        return result
    }
}
