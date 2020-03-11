package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.isUnityGeneratedProject
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*

class UnityProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {

    override fun getBestParentProjectModelNode(virtualFile: VirtualFile): ProjectModelNode? {
        if (!project.isUnityGeneratedProject())
            return super.getBestParentProjectModelNode(virtualFile)

        val host = ProjectModelViewHost.getInstance(project)

        return recursive(virtualFile, virtualFile, host) ?: super.getBestParentProjectModelNode(virtualFile)
    }

    private fun recursive(origin : VirtualFile, virtualFile: VirtualFile, host: ProjectModelViewHost): ProjectModelNode?
    {
        // when to stop going up
        val items = host.getItemsByVirtualFile(virtualFile).toList()
        if (items.filter { it.isSolutionFolder()}.any() || items.filter{it.isSolution()}.any())
            return null

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

        return recursive(origin, virtualFile.parent, host)
    }
}