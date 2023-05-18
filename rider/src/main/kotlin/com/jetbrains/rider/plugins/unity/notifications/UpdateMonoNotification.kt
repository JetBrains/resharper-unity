package com.jetbrains.rider.plugins.unity.notifications

import com.jetbrains.rd.platform.client.ProtocolProjectSession
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

class UpdateMonoNotification : LifetimedService() {
    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ProtocolProjectSession, model: FrontendBackendModel) {
            model.showInstallMonoDialog.adviseOnce(session.project.lifetime) {
                val dialog = com.jetbrains.rider.environmentSetup.EnvironmentSetupDialog(session.project, "mono")
                dialog.showAndGet()
            }
        }
    }
}