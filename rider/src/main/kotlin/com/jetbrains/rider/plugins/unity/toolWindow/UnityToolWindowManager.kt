package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.getLogger
import com.jetbrains.rd.util.reactive.whenTrue

class UnityToolWindowManager(project: Project,
                             host: UnityHost,
                             private val unityToolWindowFactory: UnityToolWindowFactory)
    : LifetimedProjectComponent(project) {
    companion object {
        private val myLogger = getLogger<UnityToolWindowManager>()
    }

    init {
        host.sessionInitialized.whenTrue(componentLifetime) {
            myLogger.info("new session")
            val context = unityToolWindowFactory.getOrCreateContext()
            val shouldReactivateBuildToolWindow = context.isActive

            if (shouldReactivateBuildToolWindow) {
                context.activateToolWindowIfNotActive()
            }
        }

        host.onLogEvent.advise(componentLifetime) { message ->
            val context = unityToolWindowFactory.getOrCreateContext()

            context.addEvent(message)
        }

        host.model.activateUnityLogView.advise(componentLifetime){
            val context = unityToolWindowFactory.getOrCreateContext()
            context.activateToolWindowIfNotActive()
        }
    }
}