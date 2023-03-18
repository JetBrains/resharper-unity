package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.virtualFile
import com.jetbrains.rd.util.getOrCreate
import com.jetbrains.rider.model.RdCustomLocation
import com.jetbrains.rider.projectView.workspace.*
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
                    val itemKey = if (itemLocation is RdCustomLocation) itemLocation.customLocation else itemLocation.toString()
                    val item = referenceNames.getOrCreate(itemKey) {
                      ReferenceItemNode(myProject, entity.descriptor.name, virtualFile, arrayListOf())
                    }
                    item.entityReferences.add(entity.toReference())
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
}

class ReferenceItemNode(
    project: Project,
    private val referenceName: String,
    virtualFile: VirtualFile,
    override val entityReferences: ArrayList<ProjectModelEntityReference>
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
        get() = entityReferences.mapNotNull { it.getEntity(myProject) }
    override val entity: ProjectModelEntity?
        get() = entityReference?.getEntity(myProject)
    override val entityReference: ProjectModelEntityReference?
        get() = entityReferences.firstOrNull()

    // Don't show references with weird file statuses. They are files, and some will be in ignored folders
    // (e.g. Library/PackageCache)
    override fun getFileStatus(): FileStatus = FileStatus.NOT_CHANGED
}
