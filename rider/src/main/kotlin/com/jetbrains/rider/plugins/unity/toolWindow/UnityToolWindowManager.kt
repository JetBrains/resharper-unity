package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.notification.NotificationGroup
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.getLogger
import com.jetbrains.rider.util.reactive.whenTrue

class UnityToolWindowManager(project: Project,
                             private val host: UnityHost,
                             private val unityToolWindowFactory: UnityToolWindowFactory)
    : LifetimedProjectComponent(project) {
    companion object {
        val BUILD_NOTIFICATION_GROUP = NotificationGroup.toolWindowGroup("Unity Console Messages", UnityToolWindowFactory.TOOLWINDOW_ID)
        private val myLogger = getLogger<UnityToolWindowManager>()
    }

    init {
        host.sessionInitialized.whenTrue(componentLifetime) {
            myLogger.info("new session")
            val context = unityToolWindowFactory.getOrCreateContext()
            //context.clear()
            val shouldReactivateBuildToolWindow = context.isActive


            if (shouldReactivateBuildToolWindow) {
                context.activateToolWindowIfNotActive()
            }
        }

        host.logSignal.advise(componentLifetime) { message ->
            val context = unityToolWindowFactory.getOrCreateContext()

            context.addEvent(message)
        }
    }
}