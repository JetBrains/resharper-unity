package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.client.ClientProjectSession
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel

class UnityToolWindowManagerProtocolListener : SolutionExtListener<FrontendBackendModel> {
    override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
        model.unityEditorConnected.whenTrue(lifetime) {
            UnityToolWindowFactory.activateToolWindowIfNotActive(session.project)
        }

        model.consoleLogging.onConsoleLogEvent.adviseNotNull(lifetime) {
            UnityLogPanelModel.getInstance(session.project).events.addEvent(it)
        }

        model.activateUnityLogView.advise(lifetime) {
            UnityToolWindowFactory.activateToolWindowIfNotActive(session.project)
        }
    }
}