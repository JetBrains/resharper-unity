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
        if (!virtualFile.isDirectory || !UnityExplorer.getInstance(myProject).myShowProjectNames) return
        addProjects(presentation)
    }

    protected fun addProjects(presentation: PresentationData) {
        val projectNames = nodes   // One node for each project that this directory is part of
                .mapNotNull { containingProjectNode(it) }
                .map { it.name.removePrefix(UnityExplorer.DefaultProjectPrefix + "-").removePrefix(UnityExplorer.DefaultProjectPrefix) }
                .filter { it.isNotEmpty() }
                .sortedWith(String.CASE_INSENSITIVE_ORDER)
        if (projectNames.any()) {
            var description = projectNames.take(3).joinToString(", ")
            if (projectNames.count() > 3) {
                description += ", …"
                presentation.tooltip = "Contains files from multiple projects:\n" + projectNames.joinToString("\n")
            }
            presentation.addText(" ($description)", SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES)
        }
    }

    private fun containingProjectNode(node: IProjectModelNode): ProjectModelNode? {
        if (node is ProjectModelNode && node.isProject())
            return null

        val projectNode = node.containingProject() ?: return null

        // Show the project on the owner of the assembly definition file
        val dir = node.getVirtualFile()
        if (dir != null && hasAssemblyDefinitionFile(dir)) {
            return projectNode
        }

        // Hide the project if we're under an assembly definition - the first .asmdef we meet is the root of this project
        if (isUnderAssemblyDefinition()) {
            return null
        }

        // These special folders aren't used in Packages
        if (isUnderAssets) {

            // This won't work if the projects are renamed by some kind of Unity plugin
            // If the project is -Editor, hide if this node is under the Editor folder
            // If the project is -firstpass, hide if this node is under Plugins, Standard Assets or Pro Standard Assets
            // If the project is -Editor-firstpass, see if this node is under an Editor folder that is itself under
            //   Plugins, Standard Assets, Pro Standard Assets
            if (projectNode.name == UnityExplorer.DefaultProjectPrefix + "-Editor" && isUnderEditorFolder()) {
                return null
            }
            if (projectNode.name == UnityExplorer.DefaultProjectPrefix + "-firstpass" && isUnderFirstpassFolder()) {
                return null
            }
            if (projectNode.name == UnityExplorer.DefaultProjectPrefix + "-Editor-firstpass") {
                val editor = findAncestor(this.parent as? FileSystemNodeBase?, "Editor")
                if (editor != null && isUnderFirstpassFolder(editor)) {
                    return null
                }
            }
        }

        return projectNode
    }

    private fun forEachAncestor(root: FileSystemNodeBase?, action: FileSystemNodeBase.() -> Boolean): FileSystemNodeBase? {
        var node: FileSystemNodeBase? = root
        while (node != null) {
            if (node.action())
                return node
            node = node.parent as? FileSystemNodeBase
        }
        return null
    }

    private fun findAncestor(root: FileSystemNodeBase?, name: String): FileSystemNodeBase? {
        return forEachAncestor(root) { this.name.equals(name, true) }
    }

    private fun isUnderEditorFolder(): Boolean {
        return findAncestor(this.parent as? FileSystemNodeBase?, "Editor") != null
    }

    private fun isUnderFirstpassFolder(root: FileSystemNodeBase? = null): Boolean {
        return forEachAncestor(root ?: this.parent as? FileSystemNodeBase?) {
            this.name.equals("Plugins", true)
                    || this.name.equals("Standard Assets", true)
                    || this.name.equals("Pro Standard Assets", true)
        } != null
    }

    private fun isUnderAssemblyDefinition(): Boolean {
        return forEachAncestor(this.parent as? FileSystemNodeBase) {
            this.virtualFile.children.any { it.extension.equals("asmdef", true) }
        } != null
    }

    private fun hasAssemblyDefinitionFile(dir: VirtualFile): Boolean {
        return dir.children.any { it.extension.equals("asmdef", true) }
    }

    private fun calculateIcon(): Icon? {
        // Under Packages, the only special folder is "Resources". As per Maxime @ Unity:
        // "Resources folders work the same in packages as under Assets, but that's mostly it. Editor folders have no
        // special semantics, Gizmos don't work, there's no StreamingAssets, no Plugins"
        if (name.equals("Resources", true)) {
            return UnityIcons.Explorer.ResourcesFolder
        }

        if (isUnderAssets) {
            if (name.equals("Editor", true) && !isUnderAssemblyDefinition()) {
                return UnityIcons.Explorer.EditorFolder
            }

            if (parent is AssetsRoot) {
                val rootSpecialIcon = when (name.toLowerCase()) {
                    "editor default resources" -> UnityIcons.Explorer.EditorDefaultResourcesFolder
                    "gizmos" -> UnityIcons.Explorer.GizmosFolder
                    "plugins" -> UnityIcons.Explorer.PluginsFolder
                    "standard assets" -> UnityIcons.Explorer.AssetsFolder
                    "pro standard assets" -> UnityIcons.Explorer.AssetsFolder
                    "streamingassets" -> UnityIcons.Explorer.StreamingAssetsFolder
                    else -> null
                }
                if (rootSpecialIcon != null) {
                    return rootSpecialIcon
                }
            }
        }

        if (hasAssemblyDefinitionFile(virtualFile)) {
            return UnityIcons.Explorer.AsmdefFolder
        }

        return virtualFile.calculateFileSystemIcon(project!!)
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
        if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension)) {
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
