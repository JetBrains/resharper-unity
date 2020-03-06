package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.ui.ConsoleView
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.notification.NotificationType
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.model.debuggerWorker.OutputMessageWithSubject
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.run.IDebuggerOutputListener

class UnityDebuggerOutputListener(val project: Project, private val host: String, private val targetName: String, private val isEditor: Boolean)
    : IDebuggerOutputListener {

    override fun onOutputMessageAvailable(message: OutputMessageWithSubject) {
        if (message.subject == OutputSubject.ConnectionError) {
            var text = "Unable to connect to $targetName"
            text += if (isEditor) {
                "\nPlease ensure 'Editor Attaching' is enabled in Unity's External Tools settings page.\n"
            }
            else {
                "\nPlease ensure that the player has 'Script Debugging' enabled and that the host '$host' is reachable.\n"
            }

            XDebuggerManagerImpl.NOTIFICATION_GROUP.createNotification(text, NotificationType.ERROR).notify(project)

            val debuggerManager = project.getComponent(XDebuggerManager::class.java)
            val debugProcess = debuggerManager.currentSession?.debugProcess as? DotNetDebugProcess

            if (debugProcess != null) {
                val console = debugProcess.console
                (console as? ConsoleView)?.print("\n" + text, when (message.type) {
                    OutputType.Info -> ConsoleViewContentType.NORMAL_OUTPUT
                    OutputType.Warning -> ConsoleViewContentType.LOG_WARNING_OUTPUT
                    OutputType.Error -> ConsoleViewContentType.ERROR_OUTPUT
                })
            }
        }
        super.onOutputMessageAvailable(message)
    }
}