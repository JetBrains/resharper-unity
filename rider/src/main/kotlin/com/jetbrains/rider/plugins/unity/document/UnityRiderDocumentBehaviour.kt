package com.jetbrains.rider.plugins.unity.document

import com.intellij.openapi.project.Project
import com.intellij.platform.backend.workspace.virtualFile
import com.jetbrains.rider.document.RiderDocumentBehaviour
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.containingProjectEntity

class UnityRiderDocumentBehaviour: RiderDocumentBehaviour() {
    override fun skipInDocumentOpening(project: Project, entity: ProjectModelEntity): Boolean {
        val name = entity.containingProjectEntity()?.url?.virtualFile?.name ?: return false
        return name.endsWith(".Player.csproj", true)
    }
}