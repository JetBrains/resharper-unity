package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.actions.properties.EditPropertiesAction

class ShowReferencePropertiesAction : AnAction("Properties"), DumbAware {

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabledAndVisible = getReference(e) != null
    }

    override fun actionPerformed(e: AnActionEvent) {
        val reference = getReference(e) ?: return
        val project = e.project ?: return
        val key = reference.keys.firstOrNull() ?: return
        val node = ProjectModelViewHost.getInstance(project).getItemById(key.id) ?: return
        EditPropertiesAction.showDialog(node)
    }

    private fun getReference(e: AnActionEvent): UnityExplorerNode.ReferenceItem? {
        return e.getData(UnityExplorer.SELECTED_REFERENCE_KEY)
    }
}