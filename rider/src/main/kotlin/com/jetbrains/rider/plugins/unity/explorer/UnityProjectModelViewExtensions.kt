package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*

class UnityProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {

    override fun getBestProjectModelNode(targetLocation: VirtualFile): ProjectModelNode? {
        val host = ProjectModelViewHost.getInstance(project)
        val node = recursiveSearch(targetLocation, host)
        if (node != null && node.getVirtualFile() == targetLocation)
            return node

        return null
    }

    override fun getBestParentProjectModelNode(targetLocation: VirtualFile): ProjectModelNode? {
        val host = ProjectModelViewHost.getInstance(project)
        return recursiveSearch(targetLocation, host) ?: super.getBestParentProjectModelNode(targetLocation)
    }

    override fun filterProjectModelNodesBeforeOperation(nodes: List<ProjectModelNode>): List<ProjectModelNode> {
        return nodes.filter { !it.containingProject()!!.name.endsWith(".Player") }
    }

    private fun recursiveSearch(virtualFile: VirtualFile?, host: ProjectModelViewHost): ProjectModelNode? {
        if (virtualFile == null) // may happen for packages outside of solution folder
            return null

        // when to stop going up
        val items = host.getItemsByVirtualFile(virtualFile)
            .map { Pair(constructNameWithPlayer(it), it) }.groupBy { a->a.first } // filter out Player projects
            .mapValues { it.value.first().second  }.values.toList()
        if (items.filter { it.isSolutionFolder() }.any()
            || items.filter { it.isSolution() }.any()) // don't forget to check File System Explorer
            return null

        //assert(items.all{it.isProjectFolder()}) {"Only ProjectFolders are expected."}

        // one of the predefined projects
        if (items.count() > 1) {
            // predefined projects in the following order
            val predefinedProjectNames = arrayOf(
                UnityExplorer.DefaultProjectPrefix,
                UnityExplorer.DefaultProjectPrefix + "-firstpass",
                UnityExplorer.DefaultProjectPrefix + "-Editor",
                UnityExplorer.DefaultProjectPrefix + "-Editor-firstpass"
            )

            for (name in predefinedProjectNames) {
                for (node in items) {
                    if (node.containingProject()?.name.equals(name))
                        return node
                }
            }
        }

        // we are in a folder, which contains scripts - choose same node as scripts
        val candidates = items.filter { node -> node.getChildren().any { it.isProjectFile() } }
        if (candidates.count() == 1)
            return candidates.single()

        return recursiveSearch(virtualFile.parent, host)
    }

    fun constructNameWithPlayer(node:ProjectModelNode):String{
        val name = node.containingProject()!!.name
        if (name.endsWith(".Player"))
            return name
        else
            return name+".Player"
    }
}