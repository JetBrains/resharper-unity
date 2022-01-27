package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.process.KillableProcessHandler
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessListener
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.rd.util.withLongBackgroundContext
import com.intellij.openapi.util.Key
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.tryWriteMessageToConsoleView
import com.jetbrains.rider.model.debuggerWorker.DebuggerWorkerModel
import com.jetbrains.rider.model.debuggerWorker.OutputMessageWithSubject
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachProfileState
import com.jetbrains.rider.run.*
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.util.NetUtils

/**
 * [RunProfileState] to launch a Unity executable (player or editor) and attach the debugger
 */
class UnityExeDebugProfileState(private val exeConfiguration : UnityExeConfiguration,
                                private val remoteConfiguration: RemoteConfiguration,
                                executionEnvironment: ExecutionEnvironment,
                                isEditor: Boolean = false)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, "Unity Executable", isEditor) {

    override val consoleKind: ConsoleKind = ConsoleKind.Normal

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        val runCmd = super.createWorkerRunInfo(lifetime, helper, port)

        remoteConfiguration.listenPortForConnections = true
        remoteConfiguration.port = NetUtils.findFreePort(500013, setOf(port))
        remoteConfiguration.address = "127.0.0.1"

        return runCmd
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        val runCommandLine = createEmptyConsoleCommandLine(exeConfiguration.parameters.useExternalConsole)
            .withEnvironment(exeConfiguration.parameters.envs)
            .withEnvironment("MONO_ARGUMENTS", "--debugger-agent=transport=dt_socket,address=127.0.0.1:${remoteConfiguration.port},server=n,suspend=y")
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

        // Once the frontend starts the debugger worker process, we'll start the Unity exe, and terminate it when the
        // debug session ends
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
                // might be worth to add the following line to let platform handle the target process, but it doesn't work, so we manually terminate targetProcessHandler by lifetime
                // see also RIDER-3800 Add possibility to detach/attach from process, which was Run in Rider
                // workerProcessHandler.attachTargetProcess(targetProcessHandler)
                super.startNotified(event)
            }
        })

        return monoConnectResult
    }
}
