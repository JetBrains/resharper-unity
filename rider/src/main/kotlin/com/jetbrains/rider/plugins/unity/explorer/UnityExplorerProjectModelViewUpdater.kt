package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.*
import com.intellij.ui.tree.TreeVisitor
import com.jetbrains.rider.model.RdProjectDescriptor
import com.jetbrains.rider.model.RdSolutionDescriptor
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageManagerListener
import com.jetbrains.rider.projectView.ProjectModelViewUpdater
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.views.SolutionViewVisitor
import com.jetbrains.rider.util.idea.application

class UnityExplorerProjectModelViewUpdater(project: Project) : ProjectModelViewUpdater(project) {

    private val pane: UnityExplorer? by lazy { UnityExplorer.tryGetInstance(project) }

    init {
        // Invoke later so that we avoid a circular dependency between PackageManager, ProjectModelViewHost and any
        // project model view updaters (us)
        application.invokeLater {
            val packageManager = PackageManager.getInstance(project)
            packageManager.addListener(object : PackageManagerListener {
                override fun onRefresh(all: Boolean) {
                    // Update all if the PackagesRoot node needs to be added/removed
                    if (all) {
                        updateAll()
                    } else {
                        updatePackagesRoot()
                    }
                }
            })
        }
    }

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

    override fun updateAllPresentations() {
        pane?.updatePresentationsFromRoot()
    }

    private fun updateAssetsRoot(node: ProjectModelNode?) {
        // If the solution node or a project node is modified, update the Assets root to display correct additional text
        if (node?.descriptor is RdSolutionDescriptor || node?.descriptor is RdProjectDescriptor) {
            pane?.refresh(object : SolutionViewVisitor() {
                override fun visit(node: AbstractTreeNode<*>): TreeVisitor.Action {
                    if (node is AssetsRoot) {
                        return TreeVisitor.Action.INTERRUPT
                    }

                    return TreeVisitor.Action.SKIP_CHILDREN
                }
            }, false, false)
        }
    }

    private fun updatePackagesRoot() {
        // Refresh the Packages root node, but no further. This causes the tree model for Packages to refresh, which
        // updates the entire branch. We skip all other children (Assets, Scratches)
        // Note that refresh will never refresh the root node, only its children
        pane?.refresh(object : SolutionViewVisitor() {
            override fun visit(node: AbstractTreeNode<*>): TreeVisitor.Action {
                if (node is PackagesRoot) {
                    return TreeVisitor.Action.INTERRUPT
                }
                return TreeVisitor.Action.SKIP_CHILDREN
            }
        }, false, true)
    }
}
