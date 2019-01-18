package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.projectView.ViewSettings
import com.intellij.ide.scratch.ScratchProjectViewPane
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rd.util.getOrCreate
import com.jetbrains.rider.model.*
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.projectView.views.FileSystemNodeBase
import com.jetbrains.rider.projectView.views.SolutionViewRootNodeBase
import com.jetbrains.rider.projectView.views.addAdditionalText
import javax.swing.Icon

class UnityExplorerRootNode(project: Project, private val packagesManager: PackagesManager)
    : SolutionViewRootNodeBase(project) {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val assetsFolder = myProject.projectDir.findChild("Assets")!!
        val assetsNode = UnityExplorerNode.AssetsRoot(myProject, assetsFolder)

        val nodes = mutableListOf<AbstractTreeNode<*>>(assetsNode)

        if (packagesManager.hasPackages) {
            nodes.add(PackagesRoot(myProject, packagesManager))
        }

        if (ScratchProjectViewPane.isScratchesMergedIntoProjectTab()) {
            nodes.add(ScratchProjectViewPane.createRootNode(myProject, ViewSettings.DEFAULT))
        }

        return nodes
    }
}

open class UnityExplorerNode(project: Project,
                             virtualFile: VirtualFile,
                             nestedFiles: List<VirtualFile>,
                             private val isUnderAssets: Boolean)
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
        addProjects(presentation)
    }

    protected fun addProjects(presentation: PresentationData) {
        val projectNames = nodes
                .mapNotNull { it.containingProject() }
                .map { it.name.removePrefix(UnityExplorer.DefaultProjectPrefix + "-").removePrefix(UnityExplorer.DefaultProjectPrefix) }
                .filter { it.isNotEmpty() }
                .sorted()
        if (projectNames.any()) {
            var description = projectNames.take(3).joinToString(", ")
            if (projectNames.count() > 3) {
                description += ", …"
                presentation.tooltip = "Contains code from multiple projects:\n" + projectNames.joinToString(",\n")
            }
            presentation.addText(" ($description)", SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES)
        }
    }

    private fun calculateIcon(): Icon? {
        // Under Packages, the only special folder is "Resources". As per Maxime @ Unity:
        // "Resources folders work the same in packages as under Assets, but that's mostly it. Editor folders have no
        // special semantics, Gizmos don't work, there's no StreamingAssets, no Plugins"
        if (name == "Resources") {
            return UnityIcons.Explorer.ResourcesFolder
        }

        if (isUnderAssets) {
            if (name == "Editor" && !underAssemblyDefinition()) {
                return UnityIcons.Explorer.EditorFolder
            }

            if (parent is AssetsRoot) {
                val rootSpecialIcon = when (name) {
                    "Editor Default Resources" -> UnityIcons.Explorer.EditorDefaultResourcesFolder
                    "Gizmos" -> UnityIcons.Explorer.GizmosFolder
                    "Plugins" -> UnityIcons.Explorer.PluginsFolder
                    "Standard Assets" -> UnityIcons.Explorer.AssetsFolder
                    "Pro Standard Assets" -> UnityIcons.Explorer.AssetsFolder
                    "StreamingAssets" -> UnityIcons.Explorer.StreamingAssetsFolder
                    else -> null
                }
                if (rootSpecialIcon != null) {
                    return rootSpecialIcon
                }
            }
        }

        if (hasAssemblyDefinitionFile()) {
            return UnityIcons.Explorer.AsmdefFolder
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

    private fun hasAssemblyDefinitionFile(): Boolean {
        return virtualFile.children.any { it.extension.equals("asmdef", true) }
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        return UnityExplorerNode(project!!, virtualFile, nestedFiles, isUnderAssets)
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

    class ReferenceRoot(project: Project) : AbstractTreeNode<Any>(project, key), Comparable<AbstractTreeNode<*>> {

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

        override fun compareTo(other: AbstractTreeNode<*>): Int {
            if (other is UnityExplorerNode) return -1
            return 0
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
}
