package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.execution.ui.ObservableConsoleView
import com.intellij.notification.NotificationListener
import com.intellij.notification.NotificationType
import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.Logger
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.idea.pumpMessages
import com.jetbrains.rider.util.lifetime.Lifetime
import java.time.LocalDateTime

class UnityAttachToEditorProfileState(val remoteConfiguration: UnityAttachToEditorConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        var debuggerManager = executionEnvironment.project.getComponent(XDebuggerManager::class.java)
        val debugProcess = debuggerManager.currentSession!!.debugProcess as DotNetDebugProcess
        debugProcess.sessionProxy.showNotification.advise(debugProcess.sessionLifetime) {
            var message =it.output
            if (message.contains("no connection could be made because the target machine actively refused it"))
                message=message+"\n"+"Check if debug is allowed by \"Editor Attaching\" setting in Unity."
            val notification = XDebuggerManagerImpl.NOTIFICATION_GROUP.createNotification(message, if (it.isError) NotificationType.ERROR else NotificationType.WARNING)
            notification.setListener(NotificationListener.URL_OPENING_LISTENER)
            notification.notify(debugProcess.session.project)
        }

        val result = super.execute(executor, runner, workerProcessHandler)

        if (remoteConfiguration.play) {

            // wait till debugger fully attached, because entering play mode in Unity will lead to reload appdomain. it may hang, if debugger is doing smth in background
            val disposable:com.intellij.openapi.Disposable = Disposable {  }
            var time = LocalDateTime.now();
            debugProcess.debuggerOutputConsole.addChangeListener(ObservableConsoleView.ChangeListener {
                time = LocalDateTime.now();
            }, disposable)

            pumpMessages(1000) {
                LocalDateTime.now().isBefore(time.plusSeconds(1))
            }

            logger.info("Pass value to backend, which will push Unity to enter play mode.")
            lifetime.bracket(opening = {
                // pass value to backend, which will push Unity to enter play mode.
                executionEnvironment.project.solution.customData.data["UNITY_AttachEditorAndPlay"] = "true";
            }, closing = {
                executionEnvironment.project.solution.customData.data["UNITY_AttachEditorAndPlay"] = "false"
            })
        }

        return result
    }
}