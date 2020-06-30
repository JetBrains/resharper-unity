package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.DefaultExecutionResult
import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.KillableProcessHandler
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.ui.ConsoleView
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.util.SystemInfo
import com.intellij.util.io.exists
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isAlive
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
import com.jetbrains.rider.run.createEmptyConsoleCommandLine
import com.jetbrains.rider.util.idea.createNestedAsyncPromise
import org.jetbrains.concurrency.Promise
import java.io.IOException
import java.net.ServerSocket
import java.nio.file.Paths
import kotlin.math.absoluteValue

class UnityExeDebugProfileState(private val exeConfiguration : UnityExeConfiguration, private val remoteConfiguration: RemoteConfiguration,
                                executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    private val logger = Logger.getInstance(UnityExeDebugProfileState::class.java)
    private val project = executionEnvironment.project
    private lateinit var console: ConsoleView
    private lateinit var targetProcessHandler: KillableProcessHandler

    override fun execute(executor: Executor?, runner: ProgramRunner<*>): ExecutionResult? {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler): ExecutionResult {
        throw UnsupportedOperationException("Should use overload with session")
    }

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        lifetime.onTermination {
            if (!targetProcessHandler.isProcessTerminated)
                targetProcessHandler.destroyProcess()
        }

        return DefaultExecutionResult(console, workerProcessHandler)
    }

    override fun createWorkerRunCmd(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): Promise<WorkerRunInfo> {
        val result = lifetime.createNestedAsyncPromise<WorkerRunInfo>()
        val nestedLifetimeDef = lifetime.createNested()

        val runCommandLine = createEmptyConsoleCommandLine(exeConfiguration.parameters.useExternalConsole)
            .withEnvironment(exeConfiguration.parameters.envs)
            .withParentEnvironmentType(if (exeConfiguration.parameters.isPassParentEnvs) {
                GeneralCommandLine.ParentEnvironmentType.CONSOLE
            } else {
                GeneralCommandLine.ParentEnvironmentType.NONE
            })
            .withExePath(exeConfiguration.parameters.exePath)
            .withWorkDirectory(exeConfiguration.parameters.workingDirectory)
            .withParameters(exeConfiguration.parameters.programParameters)

        targetProcessHandler = if (exeConfiguration.parameters.useExternalConsole)
            ExternalConsoleMediator.createProcessHandler(runCommandLine) as KillableProcessHandler
        else
            KillableProcessHandler(runCommandLine)
        val commandLineString = runCommandLine.commandLineString
        logger.info("Process started: $commandLineString")
        targetProcessHandler.addProcessListener(object: ProcessAdapter() {
            override fun processTerminated(event: ProcessEvent) = logger.info("Process terminated: $commandLineString")
        })

        console = createConsole(exeConfiguration.parameters.useExternalConsole, targetProcessHandler, commandLineString, executionEnvironment.project)
        console.attachToProcess(targetProcessHandler)
        targetProcessHandler.startNotify()

        // Read player-connection-guid from boot.config near the exePath
        val exePath = Paths.get(exeConfiguration.parameters.exePath)
        val config = if (SystemInfo.isMac) {
            exePath.parent.parent.resolve("Resources/Data/boot.config")
        } else {
            exePath.parent.resolve(exePath.toFile().nameWithoutExtension + "_Data").resolve("boot.config")
        }
        assert(config.exists()) { "Config file $config doesn't exist." }
        val guidPrefix = "player-connection-guid="
        val ipPrefix = "player-connection-ip="
        val lines = config.toFile().readLines()
        val guid = lines.first { line -> line.startsWith(guidPrefix) }.substring(guidPrefix.length).toLong().absoluteValue
        val ips = lines.filter { line -> line.startsWith(ipPrefix) }.map { it.substring(ipPrefix.length) }.toList()

        application.executeOnPooledThread {
            UnityPlayerListener(project, {
                if (!nestedLifetimeDef.lifetime.isAlive)
                    return@UnityPlayerListener

                if (!it.isEditor && guid == it.editorId && ips.contains(it.host)) {
                    if (!it.allowDebugging)
                        result.setError("Make sure the \"Script Debugging\" is enabled for this Standalone Player.") //https://docs.unity3d.com/Manual/BuildSettings.html

                    while (isAvailable(it.debuggerPort))
                        Thread.sleep(10)

                    UIUtil.invokeLaterIfNeeded {
                        logger.trace("Connecting to Player with port: ${it.debuggerPort}")
                        remoteConfiguration.port = it.debuggerPort
                        result.setResult(createWorkerRunInfoFor(port, DebuggerWorkerPlatform.AnyCpu))
                    }
                    nestedLifetimeDef.terminate()
                }
            }, {}, nestedLifetimeDef.lifetime)
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
