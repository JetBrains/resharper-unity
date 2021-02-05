package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rd.util.assert
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.projectView.ProjectElementView
import com.jetbrains.rider.projectView.ProjectEntityView
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.workspace.*

class UnityProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {

    // this is called for rename, we should filter .Player projects and return node itself
    override fun getBestProjectModelElement(targetLocation: VirtualFile): ProjectElementView? {
        if (!project.isUnityProject())
            return null

        val workspaceModel = WorkspaceModel.getInstance(project)
        val items = filterOutItemsFromNonPrimaryProjects(workspaceModel.getProjectModelEntities(targetLocation, project).toList())

        if (items.count() == 1)
            return ProjectEntityView(project, items.single())

        return null
    }

    override fun getBestParentProjectModelNode(targetLocation: VirtualFile): ProjectModelEntity? {
        if (!project.isUnityProject())
            return null
        return recursiveSearch(targetLocation) ?: super.getBestParentProjectModelNode(targetLocation)
    }

    override fun filterProjectModelNodesBeforeOperation(entities: List<ProjectModelEntity>): List<ProjectModelEntity> {
        if (!project.isUnityProject())
            return entities

        return filterOutItemsFromNonPrimaryProjects(entities)
    }

    private fun recursiveSearch(targetLocation: VirtualFile?): ProjectModelEntity? {
        if (targetLocation == null) // may happen for packages outside of solution folder
            return null

        val items = filterOutItemsFromNonPrimaryProjects(WorkspaceModel.getInstance(project).getProjectModelEntities(targetLocation, project))

        // when to stop going up
        if (items.filter { it.isSolutionFolder() }.any()
            || items.filter { it.isSolution() }.any()) // don't forget to check File System Explorer
            return null

        assert(items.all { it.isProjectFolder() }) { "Only ProjectFolders are expected." }

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
                    if (node.containingProjectEntity()?.name.equals(name))
                        return node
                }
            }
        }

        // we are in a folder, which contains scripts - choose same node as scripts
        val candidates = items.filter { node -> node.childrenEntities.any { it.isProjectFile() } }
        if (candidates.count() == 1)
            return candidates.single()

        return recursiveSearch(targetLocation.parent)
    }

    // filter out duplicate items in Player projects
    // todo: case with main project named .Player is also possible
    private fun filterOutItemsFromNonPrimaryProjects(items: List<ProjectModelEntity>): List<ProjectModelEntity> {
        val elementsWithoutProject = items.filter { it.containingProjectEntity() == null }.toList()
        val elementsWithProject = items.filter { it.containingProjectEntity() != null }.toList()
        val elementsWithNonPlayerProject = elementsWithProject.filter { !it.containingProjectEntity()!!.name.endsWith(".Player") }.toList()
        val elementsWithPlayerProject = elementsWithProject.filter { it.containingProjectEntity()!!.name.endsWith(".Player") }.toList()

        val res = mutableListOf<ProjectModelEntity>()
        res.addAll(elementsWithoutProject)
        res.addAll(elementsWithNonPlayerProject)

        // there might be elements only with Player project
        elementsWithPlayerProject.forEach { player ->
            if (!elementsWithNonPlayerProject.any { el ->
                    el.url == player.url && el.containingProjectEntity()!!.name + ".Player" == player.containingProjectEntity()!!.name
                }) {
                res.add(player)
            }
        }

        return res
    }
}