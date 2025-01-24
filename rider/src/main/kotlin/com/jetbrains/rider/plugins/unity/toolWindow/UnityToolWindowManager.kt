package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.diagnostic.thisLogger
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

@Service(Service.Level.PROJECT)
class UnityToolWindowManager {
    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.unityEditorConnected.whenTrue(lifetime) {
                thisLogger().info("new session")
                UnityToolWindowFactory.activateToolWindowIfNotActive(session.project)
            }

            model.consoleLogging.onConsoleLogEvent.adviseNotNull(lifetime) {
                UnityToolWindowFactory.addEvent(session.project, it)
            }

            model.activateUnityLogView.advise(lifetime) {
                UnityToolWindowFactory.activateToolWindowIfNotActive(session.project)
            }
        }
    }
}