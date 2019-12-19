package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.nodes.containingProject
import java.util.*

class UnityProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {

    override fun chooseBestProjectModelNode(nodes: List<ProjectModelNode>): ProjectModelNode? {

        // one of predefined projects
        val predefinedProjectNames = arrayOf(
            UnityExplorer.DefaultProjectPrefix,
            UnityExplorer.DefaultProjectPrefix + "-firstpass",
            UnityExplorer.DefaultProjectPrefix + "-Editor",
            UnityExplorer.DefaultProjectPrefix + "-Editor-firstpass"
        )

        for (name in predefinedProjectNames) {
            for (node in nodes) {
                if (node.containingProject()?.name.equals(name))
                    return node
            }
        }

        // asmdef inside another asmdef - choose closest
        val closest = findClosest(nodes)
        if (closest != null)
            return closest

        return super.chooseBestProjectModelNode(nodes)
    }

    private fun findClosest(nodes: List<ProjectModelNode>): ProjectModelNode? {
        val queue: Queue<Pair<ProjectModelNode, ProjectModelNode>> = ArrayDeque()

        for (node in nodes) {
            queue.add(Pair(node, node))
        }
        do {
            val pair = queue.poll()
            if (predicate(pair.second))
                return pair.first

            for (n in pair.second.getChildren()) {
                queue.add(Pair(pair.first, n))
            }
        } while (pair != null)
        return null
    }

    private fun predicate(it: ProjectModelNode) =
        it.getFile()?.extension.equals("asmdef", true)
}