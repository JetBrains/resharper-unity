package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.isUnityGeneratedProject
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*

class UnityProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {

    override fun getBestParentProjectModelNode(targetLocation: VirtualFile, originalNode: ProjectModelNode?): ProjectModelNode? {
        if (!project.isUnityGeneratedProject())
            return super.getBestParentProjectModelNode(targetLocation, originalNode)

        val host = ProjectModelViewHost.getInstance(project)

        return recursiveSearch(targetLocation, host) ?: super.getBestParentProjectModelNode(targetLocation, originalNode)
    }

    private fun recursiveSearch(virtualFile: VirtualFile?, host: ProjectModelViewHost): ProjectModelNode?
    {
        if (virtualFile == null) // may happen for packages outside of solution folder
          return null

        // when to stop going up
        val items = host.getItemsByVirtualFile(virtualFile).toList()
        if (items.filter { it.isSolutionFolder()}.any()
            || items.filter{it.isSolution()}.any()) // don't forget to check File System Explorer
            return null

        assert(items.all{it.isProjectFolder()}) {"Only ProjectFolders are expected."}

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
        val candidates = items.filter { node -> node.getChildren().any {it.isProjectFile()} }
        if (candidates.count() == 1)
            return candidates.single()

        if (candidates.count()==2) { // maybe Player project
            val firstVirtualFile = candidates.first().containingProject()!!.getVirtualFile()
            val secondVirtualFile = candidates.last().containingProject()!!.getVirtualFile()
            if (firstVirtualFile!!.nameWithoutExtension + ".Player" == secondVirtualFile!!.nameWithoutExtension || firstVirtualFile.nameWithoutExtension == secondVirtualFile.nameWithoutExtension + ".Player")
                return candidates.filter { !it.containingProject()!!.getVirtualFile()!!.nameWithoutExtension.endsWith(".Player") }.single()
        }

        return recursiveSearch(virtualFile.parent, host)
    }
}