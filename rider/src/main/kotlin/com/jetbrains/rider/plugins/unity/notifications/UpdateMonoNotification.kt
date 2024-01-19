package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

@Service(Service.Level.PROJECT)
class UpdateMonoNotification {
    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.showInstallMonoDialog.adviseOnce(lifetime) {
                val dialog = com.jetbrains.rider.environmentSetup.EnvironmentSetupDialog(session.project, "mono")
                dialog.showAndGet()
            }
        }
    }
}