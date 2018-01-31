package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.notification.NotificationGroup
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.RdUnityHost
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.getLogger
import com.jetbrains.rider.util.reactive.whenTrue

class UnityToolWindowManager(project: Project,
                             private val rdUnityHost: RdUnityHost,
                             private val unityToolWindowFactory: UnityToolWindowFactory)
    : LifetimedProjectComponent(project) {
    companion object {
        val BUILD_NOTIFICATION_GROUP = NotificationGroup.toolWindowGroup("Unity Console Messages", UnityToolWindowFactory.TOOLWINDOW_ID)
        private val myLogger = getLogger<UnityToolWindowManager>()
    }

    init {
    // projectCustomDataHost.isConnected.whenTrue(componentLifetime) {
            rdUnityHost.unitySession.viewNotNull(componentLifetime) { sessionLifetime, _ ->
            myLogger.info("new session")
            val context = unityToolWindowFactory.getOrCreateContext()
            //context.clear()
            val shouldReactivateBuildToolWindow = context.isActive


            if (shouldReactivateBuildToolWindow) {
                context.activateToolWindowIfNotActive()
            }
        }

        rdUnityHost.logSignal.advise(componentLifetime) { message ->
            val context = unityToolWindowFactory.getOrCreateContext()

            context.addEvent(message)
        }
    }
}

