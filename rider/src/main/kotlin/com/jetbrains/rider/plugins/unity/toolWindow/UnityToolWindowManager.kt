package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.Solution
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.protocol.ProtocolExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

class UnityToolWindowManager : LifetimedService() {
    companion object {
        private val myLogger = Logger.getInstance(UnityToolWindowManager::class.java)
    }

    class ProtocolListener : ProtocolExtListener<Solution, FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, project: Project, parent: Solution, model: FrontendBackendModel) {
            model.unityEditorConnected.whenTrue(lifetime) {
                myLogger.info("new session")
                val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
                val shouldReactivateBuildToolWindow = context.isActive

                if (shouldReactivateBuildToolWindow) {
                    context.activateToolWindowIfNotActive()
                }
            }

            model.consoleLogging.onConsoleLogEvent.adviseNotNull(lifetime) {
                val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
                context.addEvent(it)
            }

            model.activateUnityLogView.advise(lifetime) {
                val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
                context.activateToolWindowIfNotActive()
            }
        }
    }
}