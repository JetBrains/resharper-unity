package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.*
import com.jetbrains.rider.projectView.ProjectModelViewUpdater
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.views.SolutionViewVisitor

class UnityExplorerProjectModelViewUpdater(project: Project) : ProjectModelViewUpdater(project) {
    private val pane get() = UnityExplorer.tryGetInstance(project)

    override fun update(node: ProjectModelNode?) {
        node?.getVirtualFile()?.let {
            pane?.refresh(SolutionViewVisitor.createFor(it), false, false)
        }
    }

    override fun updateWithChildren(node: ProjectModelNode?) {
        node?.getVirtualFile()?.let {
            pane?.refresh(SolutionViewVisitor.createFor(it), false, true)
        }
    }

    override fun updateWithChildren(virtualFile: VirtualFile?) {
        virtualFile?.let {
            pane?.refresh(SolutionViewVisitor.createFor(it), false, true)
        }
    }

    override fun updateAll() {
        pane?.updateFromRoot()
    }
}
