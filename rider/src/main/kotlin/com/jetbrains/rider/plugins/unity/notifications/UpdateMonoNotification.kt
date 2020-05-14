package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution

class UpdateMonoNotification(project: Project) : ProtocolSubscribedProjectComponent(project) {

    init {
        project.solution.rdUnityModel.showInstallMonoDialog.adviseOnce(componentLifetime) {
            val dialog = com.jetbrains.rider.environmentSetup.EnvironmentSetupDialog(project, "mono")
            dialog.showAndGet()
        }
    }
}