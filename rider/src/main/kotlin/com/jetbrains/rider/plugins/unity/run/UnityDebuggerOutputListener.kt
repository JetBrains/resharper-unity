package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.ui.ConsoleView
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.notification.NotificationType
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.model.debuggerWorker.OutputMessage
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.run.IDebuggerOutputListener

class UnityDebuggerOutputListener(val project: Project) : IDebuggerOutputListener {
    override fun onOutputMessageAvailable(message: OutputMessage) {

        if (message.subject == OutputSubject.ConnectionError) {
            val text = "Check \"Editor Attaching\" in Unity settings\n"
            XDebuggerManagerImpl.NOTIFICATION_GROUP.createNotification(text, NotificationType.ERROR).notify(project)

            val debuggerManager = project.getComponent(XDebuggerManager::class.java)
            val debugProcess = debuggerManager.currentSession?.debugProcess as? DotNetDebugProcess

            if (debugProcess != null) {
                val console = debugProcess.console
                (console as? ConsoleView)?.print(text, when (message.type) {
                    OutputType.Info -> ConsoleViewContentType.NORMAL_OUTPUT
                    OutputType.Warning -> ConsoleViewContentType.LOG_WARNING_OUTPUT
                    OutputType.Error -> ConsoleViewContentType.ERROR_OUTPUT
                })
            }
        }
        super.onOutputMessageAvailable(message)
    }
}