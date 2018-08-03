package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.IconLoader
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.icons.ReSharperCommonIcons
import com.jetbrains.rider.icons.ReSharperProjectModelIcons
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.projectView.solutionName
import com.jetbrains.rider.projectView.views.FileSystemNodeBase
import com.jetbrains.rider.projectView.views.SolutionViewRootNodeBase
import com.jetbrains.rider.util.getOrCreate
import javax.swing.Icon

class UnityExplorerRootNode(project: Project) : SolutionViewRootNodeBase(project) {
    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val assetsFolder = myProject.baseDir?.findChild("Assets")!!
        val rootNode = UnityExplorerNode.Root(myProject, assetsFolder)
        return mutableListOf(rootNode)
    }
}

open class UnityExplorerNode(project: Project,
                             virtualFile: VirtualFile,
                             nestedFiles: List<VirtualFile>)
    : FileSystemNodeBase(project, virtualFile, nestedFiles) {

    val nodes: Array<IProjectModelNode>
        get() {
            val nodes = ProjectModelViewHost.getInstance(myProject).getItemsByVirtualFile(virtualFile)
            if (nodes.any()) return nodes.filterIsInstance<IProjectModelNode>().toTypedArray()
            return arrayOf(super.node)
        }

    public override fun hasProblemFileBeneath() = nodes.any { (it as? ProjectModelNode)?.hasErrors() == true }

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(calculateIcon())

        // Add additional info for directories
        if (!virtualFile.isDirectory) return
        val projectNames = nodes
            .mapNotNull { it.containingProject() }
            .map { it.name.removePrefix(UnityExplorer.DefaultProjectPrefix + "-").removePrefix(UnityExplorer.DefaultProjectPrefix) }
            .filter { it.isNotEmpty() }
        if (projectNames.any()) {
            val description = projectNames.joinToString(", ")
            presentation.addText(" ($description)", SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES)
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

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        return UnityExplorerNode(project!!, virtualFile, nestedFiles)
    }

    override fun getVirtualFileChildren(): List<VirtualFile> {
        return super.getVirtualFileChildren().filter { filterNode(it) }
    }

    private fun filterNode(file: VirtualFile): Boolean {
        if (UnityExplorer.getInstance(myProject).myShowHiddenItems) {
            return true
        }

        // See https://docs.unity3d.com/Manual/SpecialFolders.html
        val extension = file.extension?.toLowerCase()
        if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension.toLowerCase())) {
            return false
        }

        val name = file.nameWithoutExtension.toLowerCase()
        if (name == "cvs" || file.name.startsWith(".") || file.name.endsWith("~")) {
            return false
        }

        return true
    }

    class Root(project: Project, virtualFile: VirtualFile)
        : UnityExplorerNode(project, virtualFile, listOf()) {

        private val referenceRoot = ReferenceRoot(project)

        override fun update(presentation: PresentationData) {
            if (!virtualFile.isValid) return
            presentation.presentableText = project!!.solutionName
            presentation.setIcon(IconLoader.getIcon("/Icons/Explorer/UnityAssets.svg"))
        }

        override fun isAlwaysExpand() = true

        override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
            val result = super.calculateChildren()
            result.add(0, referenceRoot)
            return result
        }
    }

    class ReferenceRoot(project: Project) : AbstractTreeNode<Any>(project, key), Comparable<AbstractTreeNode<*>> {

        companion object {
            val key = Any()
        }

        override fun update(presentation: PresentationData?) {
            presentation?.presentableText = "References"
            presentation?.setIcon(ReSharperCommonIcons.CompositeElement)
        }

        override fun getChildren(): MutableCollection<AbstractTreeNode<*>> {
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

            val children = arrayListOf<AbstractTreeNode<*>>()
            for ((referenceName, keys) in referenceNames) {
                children.add(ReferenceItem(project!!, referenceName, keys))
            }
            return children
        }

        override fun compareTo(other: AbstractTreeNode<*>): Int {
            if (other is UnityExplorerNode) return -1
            return 0
        }
    }

    class ReferenceItem(project: Project, private val referenceName: String, val keys: ArrayList<ProjectModelNodeKey>)
        : AbstractTreeNode<String>(project, referenceName) {

        override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
        override fun isAlwaysLeaf() = true

        override fun update(presentation: PresentationData?) {
            presentation?.presentableText = referenceName
            presentation?.setIcon(ReSharperProjectModelIcons.Assembly)
        }
    }
}