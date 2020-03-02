package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.getLogger

class UnityToolWindowManager(project: Project) : ProtocolSubscribedProjectComponent(project) {
    companion object {
        private val myLogger = getLogger<UnityToolWindowManager>()
    }

    init {
        project.solution.rdUnityModel.sessionInitialized.whenTrue(componentLifetime) {
            myLogger.info("new session")
            val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
            val shouldReactivateBuildToolWindow = context.isActive

            if (shouldReactivateBuildToolWindow) {
                context.activateToolWindowIfNotActive()
            }
        }

        UnityHost.getInstance(project).logSignal.advise(componentLifetime) { message ->
            val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()

            context.addEvent(message)
        }

        project.solution.rdUnityModel.activateUnityLogView.advise(componentLifetime){
            val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
            context.activateToolWindowIfNotActive()
        }
    }
}