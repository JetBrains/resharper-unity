package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.DefaultExecutionResult
import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.process.KillableProcessHandler
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.ui.ConsoleView
import com.intellij.openapi.diagnostic.Logger
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerWorkerPlatform
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.plugins.unity.run.UnityPlayerListener
import com.jetbrains.rider.run.ExternalConsoleMediator
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.createConsole
import com.jetbrains.rider.run.createRunCommandLine
import com.jetbrains.rider.util.idea.createNestedAsyncPromise
import org.jetbrains.concurrency.Promise
import java.io.IOException
import java.net.ServerSocket

class UnityExeAttachProfileState(private val exeConfiguration:UnityExeConfiguration, private val remoteConfiguration: RemoteConfiguration,
                              executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    private val logger = Logger.getInstance(UnityExeAttachProfileState::class.java)
    private val project = executionEnvironment.project
    private lateinit var console: ConsoleView
    private lateinit var targetProcessHandler: KillableProcessHandler
    val dotNetExecutable = exeConfiguration.parameters.toDotNetExecutable()

    override fun execute(executor: Executor?, runner: ProgramRunner<*>): ExecutionResult? {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler): ExecutionResult {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        dotNetExecutable.onProcessStarter(executionEnvironment.runProfile, workerProcessHandler)

        lifetime.onTermination {
            if (!targetProcessHandler.isProcessTerminated)
                targetProcessHandler.destroyProcess()
        }
        return DefaultExecutionResult(console, workerProcessHandler)
    }

    override fun createWorkerRunCmd(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): Promise<WorkerRunInfo> {
        val result = lifetime.createNestedAsyncPromise<WorkerRunInfo>()

        val useExternalConsole = exeConfiguration.parameters.useExternalConsole
        val commandLine = dotNetExecutable.createRunCommandLine()
        targetProcessHandler = if (useExternalConsole)
            ExternalConsoleMediator.createProcessHandler(commandLine) as KillableProcessHandler
        else
            KillableProcessHandler(commandLine)
        val commandLineString = commandLine.commandLineString
        logger.info("Process started: $commandLineString")
        targetProcessHandler.addProcessListener(object: ProcessAdapter() {
            override fun processTerminated(event: ProcessEvent) = logger.info("Process terminated: $commandLineString")
        })
        console = createConsole(useExternalConsole, targetProcessHandler, commandLineString, executionEnvironment.project)
        targetProcessHandler.startNotify()

        application.executeOnPooledThread {
            UnityPlayerListener(project, {
                if (!it.isEditor) {
                    while (isAvailable(it.debuggerPort))
                        Thread.sleep(1)

                    UIUtil.invokeLaterIfNeeded {
                        logger.trace("Connecting to Player with port: ${it.debuggerPort}")
                        remoteConfiguration.port = it.debuggerPort
                        result.setResult(createWorkerRunInfoFor(port, DebuggerWorkerPlatform.AnyCpu))
                    }
                }
            }, {}, lifetime)
        }
        return result
    }

    private fun isAvailable(port: Int): Boolean {
        var portFree = false
        try {
            ServerSocket(port).use { portFree = true }
        } catch (e: IOException) {
            portFree = false
        }
        return portFree
    }
}
