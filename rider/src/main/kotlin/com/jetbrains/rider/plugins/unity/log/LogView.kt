package com.jetbrains.rider.plugins.unity.log

import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.components.AbstractProjectComponent
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupManager
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.util.idea.ILifetimedComponent
import com.jetbrains.rider.util.idea.LifetimedComponent
import com.jetbrains.rider.util.idea.getLogger
import com.jetbrains.rider.util.reactive.viewNotNull
import java.io.File

class LogView(private val project: Project,
              private val projectCustomDataHost: ProjectCustomDataHost,
              private val logToolWindowFactory: LogToolWindowFactory)
    : AbstractProjectComponent(project), ILifetimedComponent by LifetimedComponent(project) {
    companion object {
        val BUILD_NOTIFICATION_GROUP = NotificationGroup.toolWindowGroup("Unity Console Messages", LogToolWindowFactory.TOOLWINDOW_ID)
        private val myLogger = getLogger<LogView>()

    }

    init {
        projectCustomDataHost.unitySession.viewNotNull(componentLifetime) { sessionLifetime, session ->
            //                session.result.advise(componentLifetime, { buildResultKind ->
//                    myLogger.info("result: $buildResultKind")
//                })
            myLogger.info("new session")
            sessionLifetime.add {
                myLogger.info("terminate")
            }
            val context = logToolWindowFactory.getOrCreateContext()
            context.clear()
            val shouldReactivateBuildToolWindow = context.isActive
            //context.updateProgress(session.operation)
//                session.buildEvents.advise(sessionLifetime, { events ->
//                    for(event in events) {
//                        context.addBuildEvent(event)
//                        if (event.kind == RdLogEventType.Error) {
//                            context.activateToolWindowIfNotActive()
//                        }
//                    }
//                })


            if (shouldReactivateBuildToolWindow) {
                context.activateToolWindowIfNotActive()
            }
        }


        projectCustomDataHost.logSignal.advise(componentLifetime) { message ->
            val context = logToolWindowFactory.getOrCreateContext()

                context.addOutputMessage(message.message + "\n"+message.stackTrace+"\n", message.type)
        }
    }


    private fun openLogFile(path: String) {
        val file = VfsUtil.findFileByIoFile(File(path), true)
        if (file == null)
            Notifications.Bus.notify(Notification(
                Notifications.SYSTEM_MESSAGES_GROUP_ID, "Error", "There is no such file: $path", NotificationType.ERROR), project)
        else
            FileEditorManager.getInstance(project).openFile(file, true, true)
    }

    private fun showBuildNotification(text: String, type: NotificationType) {
        Notifications.Bus.notify(
            Notification(BUILD_NOTIFICATION_GROUP.displayId, "", text, type), project)
    }
}

