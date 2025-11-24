package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.ui.ConsoleView
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.ide.BrowserUtil
import com.intellij.notification.Notification
import com.intellij.notification.NotificationAction
import com.intellij.notification.NotificationType
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.intellij.util.text.VersionComparatorUtil
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rider.debugger.dotnetDebugProcess
import com.jetbrains.rider.model.debuggerWorker.OutputMessageWithSubject
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.run.IDebuggerOutputListener

class UnityDebuggerOutputListener(val project: Project,
                                  private val host: String,
                                  private val targetName: String,
                                  private val isEditor: Boolean)
    : IDebuggerOutputListener {

    override fun onOutputMessageAvailable(message: OutputMessageWithSubject) {
        if (message.subject == OutputSubject.ConnectionError) {
            var text = UnityBundle.message("notification.content.unable.to.connect.to", targetName)

            val unityVersion: String? = UnityInstallationFinder.getInstance(project).getApplicationVersion(2)
            var url: String? = null
            text += if (unityVersion != null && VersionComparatorUtil.compare(unityVersion, "2018.2") >= 0) {
                url = "https://docs.unity3d.com/$unityVersion/Documentation/Manual/ManagedCodeDebugging.html"
                if (VersionComparatorUtil.compare(unityVersion, "6000.0") >= 0) {
                    url = "https://docs.unity3d.com/$unityVersion/Documentation/Manual/managed-code-debugging.html"
                }
                if (isEditor)
                    UnityBundle.message("notification.content.please.follow.href.debugging.in.editor.documentation")
                else
                    UnityBundle.message("notification.content.please.follow.href.debugging.in.player.documentation")
            }
            else {
                if (isEditor) {
                    UnityBundle.message(
                        "notification.content.please.ensure.editor.attaching.enabled.in.unity.s.external.tools.settings.page")
                }
                else {
                    UnityBundle.message("notification.content.please.ensure.that.player.has.script.debugging.enabled.that.host.reachable",
                                        host)
                }
            }

            val debugNotification = XDebuggerManagerImpl.getNotificationGroup().createNotification(text, NotificationType.ERROR)

            if (url != null) {
                debugNotification.addAction(object : NotificationAction(
                    UnityBundle.message("open.documentation")) {
                    override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                        BrowserUtil.browse(url)
                    }
                })
            }
            debugNotification.notify(project)

            val debuggerManager = XDebuggerManager.getInstance(project)
            val debugProcess = debuggerManager.currentSession?.dotnetDebugProcess

            if (debugProcess != null) {
                val console = debugProcess.console
                (console as? ConsoleView)?.print("\n" + text, when (message.type) {
                    OutputType.Info -> ConsoleViewContentType.NORMAL_OUTPUT
                    OutputType.Warning -> ConsoleViewContentType.LOG_WARNING_OUTPUT
                    OutputType.Error -> ConsoleViewContentType.ERROR_OUTPUT
                })
            }
        }
    }
}
