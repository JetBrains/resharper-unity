package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.UnityHost

class UpdateMonoNotification(project: Project, unityHost: UnityHost)
    : LifetimedProjectComponent(project) {

    init {
        unityHost.model.showInstallMonoDialog.adviseOnce(componentLifetime) {
            val dialog = com.jetbrains.rider.environmentSetup.EnvironmentSetupDialog(project, "mono")
            dialog.showAndGet()
        }
    }
}