package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rider.model.RdSolutionDescriptor
import com.jetbrains.rider.model.RdSolutionState
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.projectView.ProjectModelStatuses
import com.jetbrains.rider.projectView.views.addAdditionalText
import com.jetbrains.rider.projectView.views.presentSyncNode
import com.jetbrains.rider.projectView.workspace.findProjects
import com.jetbrains.rider.projectView.workspace.getSolutionEntity
import com.jetbrains.rider.projectView.workspace.impl.WorkspaceProjectsCount
import icons.UnityIcons

class AssetsRootNode(project: Project, virtualFile: VirtualFile)
    : UnityExplorerFileSystemNode(project, virtualFile, emptyList(), AncestorNodeType.Assets) {

    private val referenceRoot = ReferenceRootNode(project)
    @NlsSafe private val assets = "Assets"

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.addText(assets, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.AssetsRoot)

        val solutionEntity = WorkspaceModel.getInstance(myProject).getSolutionEntity() ?: return
        val descriptor = solutionEntity.descriptor as? RdSolutionDescriptor ?: return
        val projectsCount =  WorkspaceProjectsCount.getInstance(project).get(solutionEntity)

        if (isSolutionOrProjectsSync()) {
            presentation.presentSyncNode()
        } else {
            when (descriptor.state) {
                RdSolutionState.Default -> {
                    if (projectsCount.failed + projectsCount.unloaded > 0) {
                        presentProjectsCount(presentation, projectsCount, true)
                    }
                }
                RdSolutionState.WithErrors -> presentation.addAdditionalText(UnityBundle.message("load.failed"))
                RdSolutionState.WithWarnings -> presentProjectsCount(presentation, projectsCount, true)
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

    private fun presentProjectsCount(presentation: PresentationData, count: WorkspaceProjectsCount.ProjectsCount, showZero: Boolean) {
        if (count.total == 0 && !showZero) return

        var text: String
        val unloadedCount = count.failed + count.unloaded
        if (count.total == 1) {
            text = UnityBundle.message("one.project.count", count.total)
        } else {
            text = UnityBundle.message("many.projects.count", count.total)
        }
        if (unloadedCount > 0) {
            text += UnityBundle.message("unloaded.projects.count", unloadedCount)
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
