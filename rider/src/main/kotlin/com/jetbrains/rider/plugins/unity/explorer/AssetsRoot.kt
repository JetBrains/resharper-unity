package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rd.util.getOrCreate
import com.jetbrains.rider.model.*
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.projectView.views.addAdditionalText

class AssetsRoot(project: Project, virtualFile: VirtualFile)
    : UnityExplorerNode(project, virtualFile, listOf(), true) {

    private val referenceRoot = ReferenceRoot(project)
    private val solutionNode = ProjectModelViewHost.getInstance(project).solutionNode

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.addText("Assets", SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.AssetsRoot)

        val descriptor = solutionNode.descriptor as? RdSolutionDescriptor ?: return
        val state = getAggregateSolutionState(descriptor)
        when (state) {
            RdSolutionState.Loading -> presentation.addAdditionalText("loading...")
            RdSolutionState.Sync -> presentation.addAdditionalText("synchronizing...")
            RdSolutionState.Ready -> {
                if (descriptor.projectsCount.failed + descriptor.projectsCount.unloaded > 0) {
                    presentProjectsCount(presentation, descriptor.projectsCount, true)
                }
            }
            RdSolutionState.ReadyWithErrors -> presentation.addAdditionalText("load failed")
            RdSolutionState.ReadyWithWarnings -> presentProjectsCount(presentation, descriptor.projectsCount, true)
            else -> {}
        }
    }

    private fun getAggregateSolutionState(descriptor: RdSolutionDescriptor): RdSolutionState {
        var state = descriptor.state

        // Solution loading/synchronizing takes precedence
        if (state == RdSolutionState.Loading || state == RdSolutionState.Sync) {
            return state
        }

        state = RdSolutionState.Ready
        val children = solutionNode.getChildren(false, false)
        for (child in children) {
            if (child.isProject()) {
                val projectDescriptor = child.descriptor as? RdProjectDescriptor ?: continue

                // Aggregate project loading and sync. Loading takes precedence over sync
                if (projectDescriptor.state == RdProjectState.Loading) {
                    state = RdSolutionState.Loading
                }
                else if (projectDescriptor.state == RdProjectState.Sync && state != RdSolutionState.Loading) {
                    state = RdSolutionState.Sync
                }
            }
        }

        // Make sure we don't miss solution ReadWithErrors
        if (state == RdSolutionState.Ready) {
            state = descriptor.state
        }

        return state
    }

    private fun presentProjectsCount(presentation: PresentationData, count: RdProjectsCount, showZero: Boolean) {
        if (count.total == 0 && !showZero) return

        var text = "${count.total} ${StringUtil.pluralize("project", count.total)}"
        val unloadedCount = count.failed + count.unloaded
        if (unloadedCount > 0) {
            text += ", $unloadedCount unloaded"
        }
        presentation.addAdditionalText(text)
    }

    override fun isAlwaysExpand() = true

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val result = super.calculateChildren()
        result.add(0, referenceRoot)
        return result
    }
}

class ReferenceRoot(project: Project) : AbstractTreeNode<Any>(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "References"
        presentation.setIcon(UnityIcons.Explorer.ReferencesRoot)
    }

    override fun getChildren(): MutableCollection<AbstractTreeNode<*>> {
        val referenceNames = hashMapOf<String, ArrayList<ProjectModelNodeKey>>()
        val visitor = object : ProjectModelNodeVisitor() {
            override fun visitReference(node: ProjectModelNode): Result {
                if (node.isAssemblyReference()) {
                    val keys = referenceNames.getOrCreate(node.name) { _ -> arrayListOf() }
                    keys.add(node.key)
                }
                return Result.Stop
            }
        }
        visitor.visit(project!!)

        val children = arrayListOf<AbstractTreeNode<*>>()
        for ((referenceName, keys) in referenceNames) {
            children.add(ReferenceItem(project!!, referenceName, keys))
        }
        return children
    }
}

class ReferenceItem(project: Project, private val referenceName: String, val keys: ArrayList<ProjectModelNodeKey>)
    : AbstractTreeNode<String>(project, referenceName) {

    override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.presentableText = referenceName
        presentation.setIcon(UnityIcons.Explorer.Reference)
    }
}
