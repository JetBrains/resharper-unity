package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.client.ClientProjectSession
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

class UnityProfilerToolWindowManagerProtocolListener : SolutionExtListener<FrontendBackendModel> {
    override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
        model.unityEditorConnected.whenTrue(lifetime) {
            UnityProfilerToolWindowFactory.makeAvailable(session.project)
        }
    }
}
