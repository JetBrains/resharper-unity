package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.ui.ConsoleView
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.ide.BrowserUtil
import com.intellij.notification.NotificationType
import com.intellij.openapi.project.Project
import com.intellij.util.text.VersionComparatorUtil
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.model.debuggerWorker.OutputMessageWithSubject
import com.jetbrains.rider.model.debuggerWorker.OutputSubject
import com.jetbrains.rider.model.debuggerWorker.OutputType
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.run.IDebuggerOutputListener
import javax.swing.event.HyperlinkEvent

class UnityDebuggerOutputListener(val project: Project, private val host: String, private val targetName: String, private val isEditor: Boolean)
    : IDebuggerOutputListener {

    override fun onOutputMessageAvailable(message: OutputMessageWithSubject) {
        if (message.subject == OutputSubject.ConnectionError) {
            var text = UnityBundle.message("notification.content.unable.to.connect.to", targetName)

            val unityVersion: String? = UnityInstallationFinder.getInstance(project).getApplicationVersion(2)
            text += if (unityVersion != null && VersionComparatorUtil.compare(unityVersion, "2018.2") >= 0) {
                val url = "https://docs.unity3d.com/$unityVersion/Documentation/Manual/ManagedCodeDebugging.html"
                if (isEditor) {
                    UnityBundle.message("notification.content.please.follow.href.debugging.in.editor.documentation", url)
                } else {
                    UnityBundle.message("notification.content.please.follow.href.debugging.in.player.documentation", url)
                }
            } else {
                if (isEditor) {
                    UnityBundle.message("notification.content.please.ensure.editor.attaching.enabled.in.unity.s.external.tools.settings.page")
                } else {
                    UnityBundle.message("notification.content.please.ensure.that.player.has.script.debugging.enabled.that.host.reachable", host)
                }
            }

            val debugNotification = XDebuggerManagerImpl.getNotificationGroup().createNotification(text, NotificationType.ERROR)

            debugNotification.setListener { notification, hyperlinkEvent ->
                if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                    return@setListener

                BrowserUtil.browse(hyperlinkEvent.url)
                notification.hideBalloon()
            }

            debugNotification.notify(project)

            val debuggerManager = project.getService(XDebuggerManager::class.java)
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
    }
}
