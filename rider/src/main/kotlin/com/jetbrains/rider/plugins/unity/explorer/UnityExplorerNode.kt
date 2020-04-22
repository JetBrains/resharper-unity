package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.icons.AllIcons
import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.projectView.ViewSettings
import com.intellij.ide.scratch.ScratchProjectViewPane
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.projectView.views.FileSystemNodeBase
import com.jetbrains.rider.projectView.views.SolutionViewRootNodeBase
import icons.UnityIcons
import java.awt.Color
import javax.swing.Icon

class UnityExplorerRootNode(project: Project, private val packageManager: PackageManager)
    : SolutionViewRootNodeBase(project) {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val assetsFolder = myProject.projectDir.findChild("Assets")!!
        val assetsNode = AssetsRoot(myProject, assetsFolder)

        val nodes = mutableListOf<AbstractTreeNode<*>>(assetsNode)

        if (packageManager.hasPackages) {
            nodes.add(PackagesRoot(myProject, packageManager))
        }

        if (ScratchProjectViewPane.isScratchesMergedIntoProjectTab()) {
            nodes.add(ScratchProjectViewPane.createRootNode(myProject, ViewSettings.DEFAULT))
        }

        return nodes
    }

    override fun createComparator(): Comparator<AbstractTreeNode<*>> {
        val comparator = super.createComparator()
        return Comparator { node1, node2 ->
            val sortKey1 = getSortKey(node1)
            val sortKey2 = getSortKey(node2)

            if (sortKey1 != sortKey2) {
                return@Comparator sortKey1.compareTo(sortKey2)
            }

            comparator.compare(node1, node2)
        }
    }

    private fun getSortKey(node: AbstractTreeNode<*>): Int {
        // Nodes of the same type should be sorted as the same. Different types should be in this order (although some
        // are in different levels of the hierarchy)
        return when (node) {
            is AssetsRoot -> 1
            is PackagesRoot -> 2
            is ReferenceRoot -> 3
            is ReadOnlyPackagesRoot -> 4
            is BuiltinPackagesRoot -> 5
            is PackageNode -> 6
            is DependenciesRoot -> 7
            is DependencyItemNode -> 8
            is BuiltinPackageNode -> 9
            is UnknownPackageNode -> 100
            is UnityExplorerNode -> 1000
            else -> 10000
        }
    }
}

interface IProjectModeNodesOwner {
    val nodes: Sequence<IProjectModelNode>
}

open class UnityExplorerNode(project: Project,
                             virtualFile: VirtualFile,
                             nestedFiles: List<VirtualFile>,
                             private val isUnderAssets: Boolean,
                             private val isReadOnlyPackageFile: Boolean = false)
    : FileSystemNodeBase(project, virtualFile, nestedFiles), IProjectModeNodesOwner {

    override val nodes: Sequence<IProjectModelNode>
        get() {
            val nodes = ProjectModelViewHost.getInstance(myProject).getItemsByVirtualFile(virtualFile)
            if (nodes.any()) return nodes.asSequence()
            return sequenceOf(super.node)
        }

    public override fun hasProblemFileBeneath() = nodes.any { (it as? ProjectModelNode)?.hasErrors() == true }

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(calculateIcon())

        // Add additional info for directories
        val unityExplorer = UnityExplorer.getInstance(myProject)
        if (virtualFile.isDirectory && unityExplorer.showProjectNames) {
            addProjects(presentation)
        }

        // Add tooltip for non-imported folders (anything ending with tilde). Also, show the full name if we're hiding
        // the tilde suffix
        if (virtualFile.isDirectory && virtualFile.name.endsWith("~")) {
            var tooltip = if (presentation.tooltip.isNullOrEmpty()) "" else "<br/>"
            if (!unityExplorer.showHiddenItems) {
                tooltip += virtualFile.name + "<br/>"
            }
            presentation.tooltip = tooltip + "This folder is not imported into the asset database"
        }
    }

    override fun getName(): String {
        // Remember that *~ is a default ignore pattern for IntelliJ. Any files/folders in and under this folder won't
        // be indexed. Hopefully this comment will stop someone wasting as much time as I did.
        if (virtualFile.isDirectory && !UnityExplorer.getInstance(myProject).showHiddenItems) {
            return super.getName().removeSuffix("~")
        }
        return super.getName()
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
                presentation.tooltip = "Contains files from multiple projects:<br/>" + projectNames.joinToString("<br/>")
            }
            presentation.addText(" ($description)", SimpleTextAttributes.GRAYED_ATTRIBUTES)
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

        // Note that its only the root node that's marked as "unloaded"/not imported. Child files and folder icons are
        // rendered as normal
        if (virtualFile.name.endsWith("~") && virtualFile.isDirectory) {
            return UnityIcons.Explorer.UnloadedFolder
        }

        return try {
            // Make sure that errors fetching the icon can't kill the explorer - RIDER-43038
            virtualFile.calculateFileSystemIcon(project!!)
        } catch (ex: Throwable) {
            AllIcons.FileTypes.Any_type
        }
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        return UnityExplorerNode(project!!, virtualFile, nestedFiles, isUnderAssets, isReadOnlyPackageFile)
    }

    override fun getVirtualFileChildren(): List<VirtualFile> {
        return super.getVirtualFileChildren().filter { shouldShowVirtualFile(it) }
    }

    private fun shouldShowVirtualFile(file: VirtualFile): Boolean {
        if (UnityExplorer.getInstance(myProject).showHiddenItems) {
            return true
        }

        // See https://docs.unity3d.com/Manual/SpecialFolders.html
        val extension = file.extension?.toLowerCase()
        if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension)) {
            return false
        }

        val name = file.nameWithoutExtension.toLowerCase()
        if (name == "cvs" || file.name.startsWith(".")) {
            return false
        }

        /* Files and folders ending with '~' are ignored by the asset importer. Files with '~' are usually backup files,
           but Unity uses folders that end with '~' as a way of distributing files that are not to be imported. This is
           usually `Documentation~` inside packages (https://docs.unity3d.com/Manual/cus-layout.html), but it can also
           be used for distributing code, too. This code will not be treated as assets by Unity, but will still be added
           to the generated .csproj files to allow for use as e.g. command line tools
        */
        if (file.name.endsWith("~")) {
            return file.isDirectory && UnityExplorer.getInstance(myProject).showTildeFolders
        }

        return true
    }

    override fun getFileStatus(): FileStatus {
        // Read only package files are cached under Library, which is ignored by VCS, but it's pointless us showing them
        // as IGNORED
        if (isReadOnlyPackageFile) return FileStatus.NOT_CHANGED
        return super.getFileStatus()
    }

    override fun getFileStatusColor(status: FileStatus?): Color? {
        // NOT_CHANGED colour is discovered recursively, so if any files under this are ignored, we'd get the wrong colour
        if (isReadOnlyPackageFile && status == FileStatus.NOT_CHANGED) return status?.color
        return super.getFileStatusColor(status)
    }
}
