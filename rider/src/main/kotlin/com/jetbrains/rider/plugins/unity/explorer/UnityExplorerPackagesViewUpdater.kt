package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileEvent
import com.intellij.openapi.vfs.VirtualFileListener
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.ui.tree.TreeVisitor
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.util.idea.createNestedDisposable
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.views.SolutionViewVisitor

class UnityExplorerPackagesViewUpdater(lifetime: Lifetime, project: Project, private val pane: UnityExplorer, private val packagesManager: PackagesManager) {

    init {
        // Set up a file listener for Packages/manifest.json. If it's added, deleted or modified, refresh all
        val listener = FileListener(project)
        VirtualFileManager.getInstance().addVirtualFileListener(listener, lifetime.createNestedDisposable())

        // Update the modules node whenever the Unity editor's application path changes
        project.solution.rdUnityModel.applicationPath.advise(lifetime) {
            // Refresh the package managers cached view of the packages and refresh the built in packages node
            // We have to refresh the packages because the package data for built in packages is resolved as either
            // known or unknown, based on the built in package root
            packagesManager.refresh()
            pane.refresh(object: SolutionViewVisitor() {
                override fun visit(node: AbstractTreeNode<*>): TreeVisitor.Action {
                    if (node is PackagesRoot || node is ReadOnlyPackagesRoot) {
                        // I would expect SKIP_SIBLINGS to work here, but it causes iteration to stop. Don't know why
                        return TreeVisitor.Action.CONTINUE
                    }
                    if (node is BuiltinPackagesRoot) {
                        return TreeVisitor.Action.INTERRUPT
                    }

                    return TreeVisitor.Action.SKIP_CHILDREN
                }
            }, false, true)
        }
    }

    private fun updateUnityExplorerRoot() {
        // Refresh the packages manager cached view of the packages and update the entire tree - giving chance to add or
        // remove the Packages node
        packagesManager.refresh()
        pane.updateFromRoot()
    }

    private fun updatePackagesRoot() {
        // Refresh the packages manager cached view of the packages and update just the Packages tree
        packagesManager.refresh()
        pane.refresh(object: SolutionViewVisitor() {
            override fun visit(node: AbstractTreeNode<*>): TreeVisitor.Action {
                if (node is PackagesRoot) {
                    return TreeVisitor.Action.INTERRUPT
                }
                return TreeVisitor.Action.SKIP_CHILDREN
            }
        }, false, true)
    }

    private fun containsFile(file: VirtualFile): Boolean {
        return pane.model.root.contains(file)
    }

    private inner class FileListener(private val project: Project) : VirtualFileListener {

        override fun contentsChanged(event: VirtualFileEvent) {
            if (event.file == project.projectDir.findFileByRelativePath("Packages/manifest.json")) {
                updatePackagesRoot()
            }
            else if (event.file.name == "package.json" && containsFile(event.file)) {
                // We have to refresh from root because the package metadata might be changing dependencies, which
                // can affect version resolution and the top level list of packages
                updatePackagesRoot()
            }
        }

        override fun fileDeleted(event: VirtualFileEvent) {
            if (isPackagesFolder(event.file) || isManifestJson(event.file)) {
                updateUnityExplorerRoot()
            }
        }

        override fun fileCreated(event: VirtualFileEvent) {
            if (isPackagesFolder(event.file) || isManifestJson(event.file)) {
                updateUnityExplorerRoot()
            }
        }

        private fun isPackagesFolder(file: VirtualFile?): Boolean {
            return file != null && file.name == "Packages" && file.parent == project.projectDir
        }

        private fun isManifestJson(file: VirtualFile?): Boolean {
            return file != null && file.name == "manifest.json" && isPackagesFolder(file.parent)
        }
    }
}