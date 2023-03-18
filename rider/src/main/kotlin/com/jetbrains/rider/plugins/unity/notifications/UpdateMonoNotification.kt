package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.Solution
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.protocol.ProtocolExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

class UpdateMonoNotification : LifetimedService() {
    class ProtocolListener : ProtocolExtListener<Solution, FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, project: Project, parent: Solution, model: FrontendBackendModel) {
            model.showInstallMonoDialog.adviseOnce(project.lifetime) {
                val dialog = com.jetbrains.rider.environmentSetup.EnvironmentSetupDialog(project, "mono")
                dialog.showAndGet()
            }
        }
    }
}