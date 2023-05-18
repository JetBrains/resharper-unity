@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.tree.TreeVisitor
import com.intellij.workspaceModel.ide.WorkspaceModelChangeListener
import com.intellij.workspaceModel.ide.WorkspaceModelTopics
import com.intellij.workspaceModel.storage.VersionedStorageChange
import com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity
import com.jetbrains.rider.projectView.ProjectModelViewUpdater
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.views.SolutionViewVisitor
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity

class UnityExplorerProjectModelViewUpdater(project: Project) : ProjectModelViewUpdater(project) {

    private var cachePane : UnityExplorer? = null
    private val pane get() = cachePane ?: UnityExplorer.tryGetInstance(project).apply {
        cachePane = this
    }

    init {
        val listener = object : WorkspaceModelChangeListener {
            override fun changed(event: VersionedStorageChange) {
                val changes = event.getChanges(UnityPackageEntity::class.java)
                if (changes.any()) {
                    // Don't refresh if we've not been yet been created
                    if (pane?.tree == null) {
                        return
                    }

                    // Only update the Packages subtree, unless it's been added/removed, then update everything
                    val hasPackagesRoot = pane?.hasPackagesRoot()
                    val hasPackagesFolder = project.solutionDirectory.resolve("Packages").isDirectory
                    if (hasPackagesRoot != hasPackagesFolder) {
                        updateAll()
                    }
                    else {
                        updateFromPackagesRootNode()
                    }
                }
            }
        }
        project.messageBus.connect(project).subscribe(WorkspaceModelTopics.CHANGED, listener)
    }

    override fun update(entity: ProjectModelEntity?) {
        entity?.let {
            pane?.refresh(SolutionViewVisitor.createForRefresh(it), allowLoading = false, structure = false)
        }
    }

    override fun update(virtualFile: VirtualFile?) {
        virtualFile?.let {
            pane?.refresh(SolutionViewVisitor.createForRefresh(it), allowLoading = false, structure = false)
        }
    }

    override fun updateWithChildren(entity: ProjectModelEntity?) {
        entity?.let {
            pane?.refresh(SolutionViewVisitor.createForRefresh(it), allowLoading = false, structure = true)
        }
    }

    override fun updateWithChildren(virtualFile: VirtualFile?) {
        virtualFile?.let {
            pane?.refresh(SolutionViewVisitor.createForRefresh(it), allowLoading = false, structure = true)
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
                if (node is PackagesRootNode) {
                    return TreeVisitor.Action.INTERRUPT
                }
                return TreeVisitor.Action.SKIP_CHILDREN
            }
        }, allowLoading = false, structure = true)
    }
}
