package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.projectView.ideaInterop.RiderScratchProjectViewPane
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.views.SolutionViewRootNodeBase
import com.jetbrains.rider.projectView.views.actions.ConfigureScratchesAction

class UnityExplorerRootNode(project: Project)
    : SolutionViewRootNodeBase(project) {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val nodes = mutableListOf<AbstractTreeNode<*>>()

        val assetsFolder = myProject.solutionDirectory.resolve("Assets")
        nodes.add(AssetsRootNode(myProject, assetsFolder.toVirtualFile()!!))

        // Older Unity versions won't have a packages folder
        val packagesFolder = myProject.solutionDirectory.resolve("Packages")
        if (packagesFolder.isDirectory)
            nodes.add(PackagesRootNode(myProject, packagesFolder.toVirtualFile()!!))

        if (ConfigureScratchesAction.showScratchesInExplorer(myProject)) {
            nodes.add(RiderScratchProjectViewPane.createNode(myProject))
        }

        return nodes
    }

    override fun createComparator(): Comparator<AbstractTreeNode<*>> {
        val comparator = super.createComparator()
        return Comparator { node1, node2 ->
            val sortKey1 = getSortKey(node1)
            val sortKey2 = getSortKey(node2)

            if (sortKey1 != sortKey2) {
                return@Comparator sortKey1.compareTo(sortKey2)
            }

            comparator.compare(node1, node2)
        }
    }

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