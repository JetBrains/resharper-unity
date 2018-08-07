package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.*
import com.intellij.ui.tree.TreeVisitor
import com.jetbrains.rider.model.RdProjectDescriptor
import com.jetbrains.rider.model.RdSolutionDescriptor
import com.jetbrains.rider.projectView.ProjectModelViewUpdater
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.views.SolutionViewVisitor

class UnityExplorerProjectModelViewUpdater(project: Project) : ProjectModelViewUpdater(project) {
    private val pane get() = UnityExplorer.tryGetInstance(project)

    override fun update(node: ProjectModelNode?) {
        updateAssetsRoot(node)
        node?.getVirtualFile()?.let {
            pane?.refresh(SolutionViewVisitor.createFor(it), false, false)
        }
    }

    override fun updateWithChildren(node: ProjectModelNode?) {
        updateAssetsRoot(node)
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

    private fun updateAssetsRoot(node: ProjectModelNode?) {
        // If the solution node or a project node is modified, update the Assets root to display correct additional text
        if (node?.descriptor is RdSolutionDescriptor || node?.descriptor is RdProjectDescriptor) {
            pane?.refresh(object : SolutionViewVisitor() {
                override fun visit(node: AbstractTreeNode<*>): TreeVisitor.Action {
                    if (node is UnityExplorerNode.AssetsRoot) {
                        return TreeVisitor.Action.INTERRUPT
                    }

                    return TreeVisitor.Action.SKIP_CHILDREN
                }
            }, false, false)
        }
    }
}
