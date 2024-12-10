package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.KillableProcess
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.process.*
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.ui.ConsoleView
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.ide.BrowserUtil
import com.intellij.notification.Notification
import com.intellij.notification.NotificationAction
import com.intellij.notification.NotificationType
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.util.Key
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rd.framework.RdTaskResult
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.tryWriteMessageToConsoleView
import com.jetbrains.rider.model.debuggerWorker.OutputMessageWithSubject
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachProfileState
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.*
import com.jetbrains.rider.shared.run.withRawParameters
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.util.NetUtils
import java.nio.file.Path

/**
 * [RunProfileState] to launch a Unity executable (player or editor) and attach the debugger
 */
class UnityExeDebugProfileState(val exeConfiguration: UnityExeConfiguration,
                                private val remoteConfiguration: RemoteConfiguration,
                                executionEnvironment: ExecutionEnvironment,
                                isEditor: Boolean = false)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, "Unity Executable", isEditor) {
    private val ansiEscapeDecoder = AnsiEscapeDecoder()
    override val consoleKind: ConsoleKind = ConsoleKind.Normal

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        val runCmd = super.createWorkerRunInfo(lifetime, helper, port)

        remoteConfiguration.listenPortForConnections = true
        remoteConfiguration.port = NetUtils.findFreePort(500013, setOf(port))
        remoteConfiguration.address = "127.0.0.1"

        return runCmd
    }

    override suspend fun execute(executor: Executor,
                         runner: ProgramRunner<*>,
                         workerProcessHandler: DebuggerWorkerProcessHandler,
                         lifetime: Lifetime): ExecutionResult {
        val runCommandLine = createEmptyConsoleCommandLine(exeConfiguration.parameters.useExternalConsole)
            .withEnvironment(exeConfiguration.parameters.envs)
            .withEnvironment("MONO_ARGUMENTS",
                             "--debugger-agent=transport=dt_socket,address=127.0.0.1:${remoteConfiguration.port},server=n,suspend=y")
            .withParentEnvironmentType(if (exeConfiguration.parameters.isPassParentEnvs) {
                GeneralCommandLine.ParentEnvironmentType.CONSOLE
            } else {
                GeneralCommandLine.ParentEnvironmentType.NONE
            })
            .withExePath(exeConfiguration.parameters.exePath)
            .withWorkingDirectory(Path.of(exeConfiguration.parameters.workingDirectory))
            .withRawParameters(exeConfiguration.parameters.programParameters)

        val commandLineString = runCommandLine.commandLineString

        val monoConnectResult = super.execute(executor, runner, workerProcessHandler)

        if (exeConfiguration.name == DefaultRunConfigurationGenerator.RUN_DEBUG_STANDALONE_CONFIGURATION_NAME) {
            // check if scripting backend is IL2CPP, only start the game and show red balloon
            val frontendBackendModel = executionEnvironment.project.solution.frontendBackendModel
            val res = frontendBackendModel.getScriptingBackend.start(lifetime, Unit)
            res.result.adviseNotNullOnce(lifetime) {
                if (it is RdTaskResult.Fault) {
                    thisLogger().warn("getScriptingBackend failed with ${it.error}")
                    return@adviseNotNullOnce
                }
                if (it.unwrap() == 1) {
                    val message = UnityBundle.message("debugging.il2cpp.backend.only.possible.with.attach")

                    val notification = XDebuggerManagerImpl.getNotificationGroup().createNotification(message, NotificationType.ERROR)
                    notification.addAction(object : NotificationAction(
                        UnityBundle.message("read.more")) {
                        override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                            val url = "https://github.com/JetBrains/resharper-unity/wiki/Troubleshooting-debugging-Unity-players#didnt-find-the-associated-module-for-the-breakpoint"
                            BrowserUtil.browse(url)
                        }
                    }).notify(executionEnvironment.project)
                }
            }
        }

        // Once the frontend starts the debugger worker process, we'll start the Unity exe, and terminate it when the
        // debug session ends
        workerProcessHandler.addProcessListener(object : ProcessAdapter() {
            override fun startNotified(event: ProcessEvent) {
                workerProcessHandler.workerModel.activeSession.adviseNotNullOnce(lifetime){
                    it.initialized.adviseNotNullOnce(lifetime){
                        val targetProcessHandler = if (exeConfiguration.parameters.useExternalConsole)
                            ExternalConsoleMediator.createProcessHandler(runCommandLine)
                        else
                            KillableProcessHandler(runCommandLine)


                        lifetime.onTermination {
                            if (!targetProcessHandler.isProcessTerminated) {
                                (targetProcessHandler as KillableProcess).killProcess()
                            }
                        }

                        targetProcessHandler.addProcessListener(object : ProcessListener {
                            override fun onTextAvailable(processEvent: ProcessEvent, key: Key<*>) {
                                ansiEscapeDecoder.escapeText(processEvent.text, ProcessOutputTypes.STDOUT) { textChunk, attributes ->
                                    val chunkContentType = ConsoleViewContentType.getConsoleViewType(attributes)
                                    (monoConnectResult.executionConsole as? ConsoleView)?.print(textChunk, chunkContentType)
                                }
                            }

                            override fun processTerminated(processEvent: ProcessEvent) {
                                monoConnectResult.executionConsole.tryWriteMessageToConsoleView(OutputMessageWithSubject(
                                    output = UnityBundle.message("process.0.terminated.with.exit.code.1", commandLineString,
                                                                 processEvent.exitCode.toString()) + "\r\n", type = OutputType.Warning,
                                    subject = OutputSubject.Default))
                            }

                            override fun startNotified(processEvent: ProcessEvent) {
                                monoConnectResult.executionConsole.tryWriteMessageToConsoleView(OutputMessageWithSubject(
                                    UnityBundle.message("process.0.started", commandLineString) + "\r\n", OutputType.Info, OutputSubject.Default))
                            }
                        })
                        targetProcessHandler.startNotify()
                    }
                }

                // might be worth to add the following line to let platform handle the target process, but it doesn't work, so we manually terminate targetProcessHandler by lifetime
                // see also RIDER-3800 Add possibility to detach/attach from process, which was Run in Rider
                // workerProcessHandler.attachTargetProcess(targetProcessHandler)
                super.startNotified(event)
            }
        })

        return monoConnectResult
    }
}
