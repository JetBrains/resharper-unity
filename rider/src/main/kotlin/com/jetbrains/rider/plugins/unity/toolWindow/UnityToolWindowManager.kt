package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.solution

class UnityToolWindowManager(project: Project) : ProtocolSubscribedProjectComponent(project) {

    companion object {
        private val myLogger = Logger.getInstance(UnityToolWindowManager::class.java)
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