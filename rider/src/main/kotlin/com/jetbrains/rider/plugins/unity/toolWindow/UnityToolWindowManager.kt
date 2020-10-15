package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.projectView.solution

class UnityToolWindowManager(project: Project) : ProtocolSubscribedProjectComponent(project) {

    companion object {
        private val myLogger = Logger.getInstance(UnityToolWindowManager::class.java)
    }

    init {
        project.solution.frontendBackendModel.unityEditorConnected.whenTrue(projectComponentLifetime) {
            myLogger.info("new session")
            val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
            val shouldReactivateBuildToolWindow = context.isActive

            if (shouldReactivateBuildToolWindow) {
                context.activateToolWindowIfNotActive()
            }
        }

        FrontendBackendHost.getInstance(project).logSignal.advise(projectComponentLifetime) { message ->
            val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
            context.addEvent(message)
        }

        project.solution.frontendBackendModel.activateUnityLogView.advise(projectComponentLifetime){
            val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
            context.activateToolWindowIfNotActive()
        }
    }
}