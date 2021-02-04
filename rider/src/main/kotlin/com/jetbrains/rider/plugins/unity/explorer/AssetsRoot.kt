package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.intellij.workspaceModel.ide.impl.virtualFile
import com.jetbrains.rd.util.getOrCreate
import com.jetbrains.rider.model.*
import com.jetbrains.rider.projectView.ProjectModelStatuses
import com.jetbrains.rider.projectView.views.addAdditionalText
import com.jetbrains.rider.projectView.views.presentSyncNode
import com.jetbrains.rider.projectView.workspace.*
import icons.UnityIcons

class AssetsRoot(project: Project, virtualFile: VirtualFile)
    : UnityExplorerNode(project, virtualFile, listOf(), AncestorNodeType.Assets) {

    private val referenceRoot = ReferenceRoot(project)

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

class ReferenceRoot(project: Project) : AbstractTreeNode<Any>(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "References"
        presentation.setIcon(UnityIcons.Explorer.ReferencesRoot)
    }

    override fun getChildren(): MutableCollection<AbstractTreeNode<*>> {
        val referenceNames = hashMapOf<String, ReferenceItemNode>()
        val visitor = object : ProjectModelEntityVisitor() {
            override fun visitReference(entity: ProjectModelEntity): Result {
                if (entity.isAssemblyReference()) {
                    val virtualFile = entity.url?.virtualFile
                    if (virtualFile != null) {
                        val item = referenceNames.getOrCreate(entity.descriptor.location.toString(), {
                            ReferenceItemNode(project!!, entity.descriptor.name, virtualFile, arrayListOf())
                        })
                        item.entityReferences.add(entity.toReference())
                    }
                }
                return Result.Stop
            }
        }
        visitor.visit(project!!)

        val children = arrayListOf<AbstractTreeNode<*>>()
        for ((_, item) in referenceNames) {
            children.add(item)
        }
        return children
    }
}

class ReferenceItemNode(
    project: Project,
    private val referenceName: String,
    virtualFile: VirtualFile,
    override val entityReferences: ArrayList<ProjectModelEntityReference>
) : UnityExplorerNode(project, virtualFile, listOf(), AncestorNodeType.Assets) {

    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.presentableText = referenceName
        presentation.setIcon(UnityIcons.Explorer.Reference)
    }

    override fun navigate(requestFocus: Boolean) {
        // the same VirtualFile may be added as a file inside Assets folder, so simple click on the reference would jump to that file
    }

    // Allows View In Assembly Explorer and Properties actions to work
    override val entities: List<ProjectModelEntity>
        get() = entityReferences.mapNotNull { it.getEntity(project!!) }

    override val entity: ProjectModelEntity?
        get() = entities.firstOrNull()
    override val entityReference: ProjectModelEntityReference?
        get() = entityReferences.firstOrNull()
}
