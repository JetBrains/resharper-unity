package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

class UnityToolWindowManager : LifetimedService() {
    companion object {
        private val myLogger = Logger.getInstance(UnityToolWindowManager::class.java)
    }

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.unityEditorConnected.whenTrue(lifetime) {
                myLogger.info("new session")
                val context = UnityToolWindowFactory.getInstance(session.project).getOrCreateContext()
                val shouldReactivateBuildToolWindow = context.isActive

                if (shouldReactivateBuildToolWindow) {
                    context.activateToolWindowIfNotActive()
                }
            }

            model.consoleLogging.onConsoleLogEvent.adviseNotNull(lifetime) {
                val context = UnityToolWindowFactory.getInstance(session.project).getOrCreateContext()
                context.addEvent(it)
            }

            model.activateUnityLogView.advise(lifetime) {
                val context = UnityToolWindowFactory.getInstance(session.project).getOrCreateContext()
                context.activateToolWindowIfNotActive()
            }
        }
    }
}