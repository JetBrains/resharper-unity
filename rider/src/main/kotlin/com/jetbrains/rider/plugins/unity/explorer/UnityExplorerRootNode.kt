package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.ProjectView
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ideaInterop.RiderScratchProjectViewPane
import com.jetbrains.rider.projectView.utils.compareFiles
import com.jetbrains.rider.projectView.utils.compareNodes
import com.jetbrains.rider.projectView.views.SolutionViewRootNodeBase
import com.jetbrains.rider.projectView.views.actions.ConfigureScratchesAction

class UnityExplorerRootNode(project: Project)
    : SolutionViewRootNodeBase(project) {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val nodes = mutableListOf<AbstractTreeNode<*>>()

        val assetsFolder = myProject.projectDir.findChild("Assets")!!
        nodes.add(AssetsRootNode(myProject, assetsFolder))

        // Older Unity versions won't have a packages folder
        val packagesFolder = myProject.projectDir.findChild("Packages")
        if (packagesFolder?.exists() == true) {
            nodes.add(PackagesRootNode(myProject, packagesFolder))
        }

        if (ConfigureScratchesAction.showScratchesInExplorer(myProject)) {
            nodes.add(RiderScratchProjectViewPane.createNode(myProject))
        }

        return nodes
    }

    override fun createComparator(): Comparator<AbstractTreeNode<*>> {
        return UnityExplorerComparator(project)
    }

    private class UnityExplorerComparator(val project: Project) : Comparator<AbstractTreeNode<*>> {
        val projectView = ProjectView.getInstance(project)!!

        override fun compare(node1: AbstractTreeNode<*>, node2: AbstractTreeNode<*>): Int {
            val sortKey1 = getSortKey(node1)
            val sortKey2 = getSortKey(node2)

            if (sortKey1 != sortKey2) {
                return sortKey1.compareTo(sortKey2)
            }

            if (node1 is UnityExplorerFileSystemNode && node2 is UnityExplorerFileSystemNode) {
                // Unity explorer mostly follows filesystem and Unity project structure is also mostly bound to filesystem (notable exception: Editor folders, but it doesn't matter here)
                // So we want to apply normal file sorting rather than go with the standard compareNodes, which contains more complex logic designed to handle all kinds of .NET solution
                // structures and doesn't always apply well here. For example: with Unity's new MSBuild-based compilation, the actual C# (sub-)projects might be placed next to normal files,
                // both having a single ProjectModelEntity, making compareNodes go with entity comparison which does not assume projects to be folders, so "folder-on-top" doesn't work
                // and we end up with mixed order. We sidestep all such issues with direct compareFiles and only using compareNodes as a fallback for any non-file situations that are not
                // handled by sortKeys above (not sure if there are many left)
                val isFoldersOnTop = projectView.isFoldersAlwaysOnTop(UnityExplorer.ID)
                val sortKey = projectView.getSortKey(UnityExplorer.ID)
                return compareFiles(node1.virtualFile, node2.virtualFile, isFoldersOnTop, sortKey)
            }

            return compareNodes(project, node1, node2)
        }

        companion object {
            private fun getSortKey(node: AbstractTreeNode<*>): Int {
                // Nodes of the same type should be sorted as the same. Different types should be in this order (although some
                // are in different levels of the hierarchy)
                return when (node) {
                    is AssetsRootNode -> 1
                    is PackagesRootNode -> 2
                    is ReferenceRootNode -> 3
                    is ReadOnlyPackagesRootNode -> 4
                    is BuiltinPackagesRootNode -> 5
                    is PackageNode -> 6
                    is PackageDependenciesRoot -> 7
                    is PackageDependencyItemNode -> 8
                    is BuiltinPackageNode -> 9
                    is UnknownPackageNode -> 100
                    is UnityExplorerFileSystemNode -> 1000
                    else -> 10000
                }
            }
        }
    }
}