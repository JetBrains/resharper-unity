package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.components.serviceIfCreated
import com.intellij.openapi.fileEditor.FileDocumentManagerListener
import com.intellij.openapi.project.ProjectManager
import com.intellij.util.application
import com.jetbrains.rider.model.rdShellModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.protocol.protocolHostIfExists

class UnityFileDocumentManagerListener : FileDocumentManagerListener {

    // RIDER-62051 Save-file-when-switching-application-and-different-monitors
    override fun beforeAllDocumentsSaving() {
        val projectManager = serviceIfCreated<ProjectManager>() ?: return
        val openedUnityProjects = projectManager.openProjects.filter { !it.isDisposed && it.isUnityProjectFolder() }.toList()
        for (project in openedUnityProjects)
            application.invokeLater {
                val isActive = project.protocolHostIfExists?.protocol?.rdShellModel?.isApplicationActive?.valueOrNull
                if (isActive != null && !isActive)
                    project.solution.frontendBackendModel.refresh.fire(false)
            }
    }
}