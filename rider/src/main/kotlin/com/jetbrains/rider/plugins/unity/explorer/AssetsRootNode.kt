package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rider.model.RdProjectsCount
import com.jetbrains.rider.model.RdSolutionDescriptor
import com.jetbrains.rider.model.RdSolutionState
import com.jetbrains.rider.projectView.ProjectModelStatuses
import com.jetbrains.rider.projectView.views.addAdditionalText
import com.jetbrains.rider.projectView.views.presentSyncNode
import com.jetbrains.rider.projectView.workspace.findProjects
import com.jetbrains.rider.projectView.workspace.getSolutionEntity
import icons.UnityIcons

@Suppress("UnstableApiUsage")
class AssetsRootNode(project: Project, virtualFile: VirtualFile)
    : UnityExplorerFileSystemNode(project, virtualFile, emptyList(), AncestorNodeType.Assets) {

    private val referenceRoot = ReferenceRootNode(project)

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.addText("Assets", SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.AssetsRoot)

        val solutionEntity = WorkspaceModel.getInstance(myProject).getSolutionEntity() ?: return
        val descriptor = solutionEntity.descriptor as? RdSolutionDescriptor ?: return

        if (isSolutionOrProjectsSync()) {
            presentation.presentSyncNode()
        } else {
            when (descriptor.state) {
                RdSolutionState.Default -> {
                    if (descriptor.projectsCount.failed + descriptor.projectsCount.unloaded > 0) {
                        presentProjectsCount(presentation, descriptor.projectsCount, true)
                    }
                }
                RdSolutionState.WithErrors -> presentation.addAdditionalText("load failed")
                RdSolutionState.WithWarnings -> presentProjectsCount(presentation, descriptor.projectsCount, true)
            }
        }
    }

    private fun isSolutionOrProjectsSync(): Boolean {
        val projectModelStatuses = ProjectModelStatuses.getInstance(myProject)
        if (projectModelStatuses.isSolutionInSync()) return true

        val projects = WorkspaceModel.getInstance(myProject).findProjects()
        return projects.any { project ->
            projectModelStatuses.getProjectStatus(project) != null
        }
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
