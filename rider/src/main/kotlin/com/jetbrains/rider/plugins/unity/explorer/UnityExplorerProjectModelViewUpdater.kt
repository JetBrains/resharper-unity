package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.tree.TreeVisitor
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageManagerListener
import com.jetbrains.rider.plugins.unity.util.findFile
import com.jetbrains.rider.projectView.ProjectModelViewUpdater
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.views.SolutionViewVisitor

class UnityExplorerProjectModelViewUpdater(project: Project) : ProjectModelViewUpdater(project) {

    private val pane: UnityExplorer? by lazy { UnityExplorer.tryGetInstance(project) }

    init {
        // Invoke later so that we avoid a circular dependency between PackageManager, ProjectModelViewHost and any
        // project model view updaters (us)
        application.invokeLater {
            val packageManager = PackageManager.getInstance(project)
            packageManager.addListener(object : PackageManagerListener {
                override fun onPackagesUpdated() {
                    // Don't refresh if we've not been yet been created
                    if (pane?.tree == null) {
                        return
                    }

                    // Only update the Packages subtree, unless it's been added/removed, then update everything
                    val hasPackagesRoot = pane?.hasPackagesRoot()
                    val hasPackagesFolder = project.findFile("Packages")?.isDirectory
                    if (hasPackagesRoot != hasPackagesFolder) {
                        updateAll()
                    }
                    else {
                        updateFromPackagesRootNode()
                    }
                }
            })
        }
    }

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
        // This is called when project or solution nodes change, and invalidate the entire model, so our extra
        // presentation text (project name, "loading...", etc.) will be correctly updated automatically
        pane?.updateFromRoot()
    }

    override fun updateAllPresentations() {
        pane?.updatePresentationsFromRoot()
    }

    private fun updateFromPackagesRootNode() {
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
