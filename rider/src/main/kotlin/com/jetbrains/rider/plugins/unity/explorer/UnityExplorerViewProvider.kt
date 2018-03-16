package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.ProjectModelViewProviderBase
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.nodes.isAssemblyReference

class UnityExplorerViewProvider(project: Project) : ProjectModelViewProviderBase(project) {
    override fun getProjectViewPaneId() = UnityExplorer.ID

    override fun updateWithChildren(node: ProjectModelNode?) {
        if (node?.isAssemblyReference() == true) {
            treeBuilder?.queueUpdateFrom(UnityExplorerNode.ReferenceRoot.key, true, true)
        }
        super.updateWithChildren(node)
    }
}