package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.projectView.ProjectViewNode
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.IconLoader
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.icons.ReSharperCommonIcons
import com.jetbrains.rider.icons.ReSharperProjectModelIcons
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.fileSystem.FileSystemNodeBase
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.projectView.solutionName
import com.jetbrains.rider.util.getOrCreate
import javax.swing.Icon

open class UnityExplorerNode(project: Project, virtualFile: VirtualFile, private val owner: UnityExplorer)
    : FileSystemNodeBase(project, virtualFile) {

    val nodes = ProjectModelViewHost.getInstance(project).getItemsByVirtualFile(virtualFile)

    public override fun hasProblemFileBeneath() = nodes.any { it.hasErrors() }

    override fun compareTo(other: ProjectViewNode<*>): Int {
        if (other !is UnityExplorerNode){
            return 1
        }
        return super.compareTo(other)
    }

    override fun update(presentation: PresentationData?) {
        if (!virtualFile.isValid) return
        presentation?.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation?.setIcon(calculateIcon())

        // Add additional info for directories
        if (!virtualFile.isDirectory) return
        val projectNames = nodes
            .mapNotNull { it.containingProject() }
            .map { it.name.removePrefix(UnityExplorer.DefaultProjectPrefix + "-").removePrefix(UnityExplorer.DefaultProjectPrefix) }
            .filter { it.isNotEmpty() }
        if (projectNames.any()) {
            val description = projectNames.joinToString(", ")
            presentation?.addText(" ($description)", SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES)
        }
    }

    private fun calculateIcon(): Icon? {
        val globalSpecialIcon = when (name) {
            "Editor" -> IconLoader.getIcon("/Icons/Explorer/FolderEditor.svg")
            "Resources" -> IconLoader.getIcon("/Icons/Explorer/FolderResources.svg")
            else -> null
        }
        if (globalSpecialIcon != null && !underAssemblyDefinition()) {
            return globalSpecialIcon
        }

        if (parent is Root) {
            val rootSpecialIcon = when (name) {
                "Editor Default Resources" -> IconLoader.getIcon("/Icons/Explorer/FolderEditorResources.svg")
                "Gizmos" -> IconLoader.getIcon("/Icons/Explorer/FolderGizmos.svg")
                "Plugins" -> IconLoader.getIcon("/Icons/Explorer/FolderPlugins.svg")
                "Standard Assets" -> IconLoader.getIcon("/Icons/Explorer/FolderAssets.svg")
                "Pro Standard Assets" -> IconLoader.getIcon("/Icons/Explorer/FolderAssets.svg")
                "StreamingAssets" -> IconLoader.getIcon("/Icons/Explorer/FolderStreamingAssets.svg")
                else -> null
            }
            if (rootSpecialIcon != null) {
                return rootSpecialIcon
            }
        }

        return virtualFile.calculateFileSystemIcon(project!!)
    }

    private fun underAssemblyDefinition(): Boolean {
        // Fix for #380
        var parent = this.parent
        while (parent != null && parent is UnityExplorerNode) {
            if (parent.virtualFile.children.any { it.extension.equals("asmdef", true) }) {
                return true
            }

            parent = parent.parent
        }
        return false
    }

    override fun createNode(virtualFile: VirtualFile): FileSystemNodeBase {
        return UnityExplorerNode(project!!, virtualFile, owner)
    }

    override fun filterNode(node: FileSystemNodeBase): Boolean {
        if (owner.myShowHiddenItems) {
            return true
        }

        // See https://docs.unity3d.com/Manual/SpecialFolders.html
        val file = node.virtualFile

        val extension = file.extension?.toLowerCase()
        if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension.toLowerCase())) {
            return false
        }

        val name = file.nameWithoutExtension.toLowerCase()
        if (name == "cvs" || file.name.startsWith(".") || file.nameWithoutExtension.endsWith("~")) {
            return false
        }

        return true
    }

    class Root(project: Project, virtualFile: VirtualFile, owner: UnityExplorer)
        : UnityExplorerNode(project, virtualFile, owner) {

        private val referenceRoot = ReferenceRoot(project)

        override fun update(presentation: PresentationData?) {
            if (!virtualFile.isValid) return
            presentation?.presentableText = project!!.solutionName
            presentation?.setIcon(IconLoader.getIcon("/Icons/Explorer/UnityAssets.svg"))
        }

        override fun isAlwaysExpand() = true

        override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> {
            val children = arrayListOf<AbstractTreeNode<Any>>()
            children.add(referenceRoot as AbstractTreeNode<Any>)
            children.addAll(super.getChildren())
            return children
        }
    }

    class ReferenceRoot(project: Project) : ProjectViewNode<Any>(project, key, null), Comparable<ProjectViewNode<*>> {

        companion object {
            val key = Any()
        }

        override fun contains(virtualFile: VirtualFile) = false
        override fun canRepresent(element: Any?): Boolean = false
        override fun compareTo(other: ProjectViewNode<*>) = -1
        override fun getSortKey() = this

        override fun update(presentation: PresentationData?) {
            presentation?.presentableText = "References"
            presentation?.setIcon(ReSharperCommonIcons.CompositeElement)
        }

        override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> {
            val referenceNames = hashMapOf<String, ArrayList<ProjectModelNodeKey>>()
            val visitor = object : ProjectModelNodeVisitor() {
                override fun visitReference(node: ProjectModelNode): Result {
                    if (node.isAssemblyReference()) {
                        val keys = referenceNames.getOrCreate(node.name, { _ -> arrayListOf() })
                        keys.add(node.key)
                    }
                    return Result.Stop
                }
            }
            visitor.visit(project!!)

            val children = arrayListOf<AbstractTreeNode<Any>>()
            for ((referenceName, keys) in referenceNames) {
                children.add(ReferenceItem(project!!, referenceName, keys) as AbstractTreeNode<Any>)
            }
            return children
        }
    }

    class ReferenceItem(project: Project, private val referenceName: String, val keys : ArrayList<ProjectModelNodeKey>)
        : ProjectViewNode<String>(project, referenceName, null) {

        override fun contains(virtualFile: VirtualFile) = false
        override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
        override fun canRepresent(element: Any?): Boolean = false
        override fun isAlwaysLeaf() = true

        override fun update(presentation: PresentationData?) {
            presentation?.presentableText = referenceName
            presentation?.setIcon(ReSharperProjectModelIcons.Assembly)
        }
    }
}