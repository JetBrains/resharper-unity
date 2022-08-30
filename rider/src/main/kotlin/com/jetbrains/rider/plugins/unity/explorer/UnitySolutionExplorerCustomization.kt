package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.isUnityGeneratedProject
import com.jetbrains.rider.plugins.unity.ui.UnityUIManager
import com.jetbrains.rider.plugins.unity.ui.hasTrueValue
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerCustomization
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity

class UnitySolutionExplorerCustomization(project: Project) : SolutionExplorerCustomization(project) {

    companion object {
        private val actionIds = listOf(
            "NewJavaScriptFile",
            "NewTypeScriptFile",
            "NewStylesheetFile",
            "NewHtmlFile")
    }

    override fun supportReferenceModifications(projectEntity: ProjectModelEntity): Boolean {
        if (isUnityGeneratedAndMinimizedUI()) return false
        return super.supportReferenceModifications(projectEntity)
    }

    override fun supportSolutionModifications(): Boolean {
        if (isUnityGeneratedAndMinimizedUI()) return false
        return super.supportSolutionModifications()
    }

    override fun supportNugetModifications(): Boolean {
        if (isUnityGeneratedAndMinimizedUI()) return false
        return super.supportNugetModifications()
    }

    override fun supportIncludeExcludeModifications(): Boolean {
        if (isUnityGeneratedAndMinimizedUI()) return false
        return super.supportIncludeExcludeModifications()
    }

    override fun getNonImportantActionsForAddGroup(e: AnActionEvent): List<String> {
        if (isUnityGeneratedAndMinimizedUI()) return actionIds
        return super.getNonImportantActionsForAddGroup(e)
    }

    private fun isUnityGeneratedAndMinimizedUI(): Boolean {
        return UnityUIManager.getInstance(project).hasMinimizedUi.hasTrueValue() && project.isUnityGeneratedProject()
    }
}