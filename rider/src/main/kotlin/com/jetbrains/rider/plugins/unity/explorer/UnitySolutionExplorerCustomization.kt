package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.isUnityGeneratedProject
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerCustomization
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity

class UnitySolutionExplorerCustomization(project: Project) : SolutionExplorerCustomization(project) {
    override fun supportReferenceModifications(projectEntity: ProjectModelEntity): Boolean {
        if (project.isUnityGeneratedProject())
            return false
        return super.supportReferenceModifications(projectEntity)
    }
}