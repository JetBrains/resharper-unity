package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.isUnityGeneratedProject
import com.jetbrains.rider.plugins.unity.ui.UnityUIManager
import com.jetbrains.rider.plugins.unity.ui.hasTrueValue
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerCustomization
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity

class UnitySolutionExplorerCustomization(project: Project) : SolutionExplorerCustomization(project) {
    override fun supportReferenceModifications(projectEntity: ProjectModelEntity): Boolean {
        if (UnityUIManager.getInstance(project).hasMinimizedUi.hasTrueValue() && project.isUnityGeneratedProject())
            return false
        return super.supportReferenceModifications(projectEntity)
    }

    override fun supportSolutionModifications(): Boolean {
        if (UnityUIManager.getInstance(project).hasMinimizedUi.hasTrueValue() && project.isUnityGeneratedProject())
            return false
        return super.supportSolutionModifications()
    }

    override fun supportNugetModifications(): Boolean {
        if (UnityUIManager.getInstance(project).hasMinimizedUi.hasTrueValue() && project.isUnityGeneratedProject())
            return false
        return super.supportNugetModifications()
    }
}