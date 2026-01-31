package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.backend.workspace.virtualFile
import com.jetbrains.rd.util.getOrCreate
import com.jetbrains.rider.model.RdCustomLocation
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.ProjectModelEntityReference
import com.jetbrains.rider.projectView.workspace.ProjectModelEntityVisitor
import com.jetbrains.rider.projectView.workspace.isAssemblyReference
import com.jetbrains.rider.projectView.workspace.toReference
import icons.UnityIcons

class ReferenceRootNode(project: Project) : AbstractTreeNode<Any>(project, key) {

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
                        val itemLocation = entity.descriptor.location
                        val itemKey = if (itemLocation is RdCustomLocation) itemLocation.customLocation.value else itemLocation.toString()
                        val item = referenceNames.getOrCreate(itemKey) {
                            ReferenceItemNode(myProject, entity.descriptor.name, virtualFile, arrayListOf())
                        }
                        item.entityPointers.add(entity.toReference())
                    }
                }
                return Result.Stop
            }
        }
        visitor.visit(myProject)

        val children = arrayListOf<AbstractTreeNode<*>>()
        for ((_, item) in referenceNames) {
            children.add(item)
        }
        return children
    }

    override fun hasProblemFileBeneath(): Boolean {
        return false // RIDER-123607 Text-lag-while-typing
    }
}

class ReferenceItemNode(
    project: Project,
    private val referenceName: String,
    virtualFile: VirtualFile,
    override val entityPointers: ArrayList<ProjectModelEntityReference>
) : UnityExplorerFileSystemNode(project, virtualFile, emptyList(), AncestorNodeType.References) {

    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.presentableText = referenceName
        presentation.setIcon(UnityIcons.Explorer.Reference)
    }

    override fun navigate(requestFocus: Boolean) {
        // the same VirtualFile may be added as a file inside Assets folder, so simple click on the reference would jump to that file
    }

    override val entities: List<ProjectModelEntity>
        get() = entityPointers.mapNotNull { it.getEntity(myProject) }
    override val entity: ProjectModelEntity?
        get() = entityReference?.getEntity(myProject)
    override val entityReference: ProjectModelEntityReference?
        get() = entityPointers.firstOrNull()

    // Don't show references with weird file statuses. They are files, and some will be in ignored folders
    // (e.g. Library/PackageCache)
    override fun getFileStatus(): FileStatus = FileStatus.NOT_CHANGED

    override fun hasProblemFileBeneath(): Boolean {
        return false // RIDER-123607 Text-lag-while-typing
    }
}
