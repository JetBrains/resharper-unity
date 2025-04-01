package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.openapi.fileTypes.FileTypeManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.registry.Registry
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.backend.workspace.virtualFile
import com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity
import com.jetbrains.rider.projectView.calculateFileSystemIcon
import com.jetbrains.rider.projectView.views.FileSystemNodeBase
import com.jetbrains.rider.projectView.views.NestingNode
import com.jetbrains.rider.projectView.views.SolutionViewPaneBase
import com.jetbrains.rider.projectView.views.fileSystemExplorer.FileSystemExplorerCustomization
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getProjectModelEntities
import com.jetbrains.rider.projectView.workspace.impl.WorkspaceEntityErrorsSupport
import com.jetbrains.rider.projectView.workspace.isProject
import icons.UnityIcons
import java.awt.Color
import java.util.*
import javax.swing.Icon

enum class AncestorNodeType {
    Assets,
    UserEditablePackage,
    ReadOnlyPackage,
    IgnoredFolder,
    HiddenAsset,
    References,
    FileSystem;  // A folder in Packages that isn't a package. Gets no special treatment

    companion object {
        fun fromPackageData(packageEntity: UnityPackageEntity): AncestorNodeType {
            return if (packageEntity.isEditable()) UserEditablePackage else ReadOnlyPackage
        }
    }
}

open class UnityExplorerFileSystemNode(project: Project,
                                       virtualFile: VirtualFile,
                                       nestedFiles: List<NestingNode<VirtualFile>>,
                                       protected val descendentOf: AncestorNodeType)
    : FileSystemNodeBase(project, virtualFile, nestedFiles) {

    companion object {
        // can be folder or file
        // note that children of hidden folder are not matched by this function
        fun isHiddenAsset(file: VirtualFile?): Boolean {
            if (file == null) return false
            // See https://docs.unity3d.com/Manual/SpecialFolders.html
            val extension = file.extension?.lowercase(Locale.getDefault())
            if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension)) {
                return true
            }

            val name = file.nameWithoutExtension.lowercase(Locale.getDefault())
            if (name == "cvs" || file.name.startsWith(".")) {
                return true
            }

            return file.name.endsWith("~")
        }

        private fun isDescendantOfReadOnlyPackage(node: UnityExplorerFileSystemNode?): Boolean {
            if (node == null) return false

            if (node.descendentOf == AncestorNodeType.HiddenAsset || node.descendentOf == AncestorNodeType.IgnoredFolder) {
                return isDescendantOfReadOnlyPackage(node.parent as? UnityExplorerFileSystemNode)
            }

            return node.descendentOf == AncestorNodeType.ReadOnlyPackage
        }
    }

    override val entities: List<ProjectModelEntity>
        get() = WorkspaceModel
            .getInstance(myProject)
            .getProjectModelEntities(file, myProject)
            .toList()

    public override fun hasProblemFileBeneath(): Boolean {
        return Registry.`is`("projectView.showHierarchyErrors") && entities.any {
            WorkspaceEntityErrorsSupport.getInstance(myProject).hasErrors(it)
        }
    }

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return

        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(calculateIcon())

        FileSystemExplorerCustomization.getExtensions(myProject).forEach {
            it.updateNode(presentation, file, this)
        }

        // Mark ignored file types. This is mainly so we can highlight that hidden folders are completely ignored. We
        // have lots of non-indexed files in Assets and Packages, but they still appear in Find in Files when we check
        // 'non-solution items'. Ignored file types are completely ignored, and don't show up at all.
        // Make it clear that the folder is ignored/excluded/not indexed
        val ignored = FileTypeManager.getInstance().isFileIgnored(virtualFile)
        if (ignored || descendentOf == AncestorNodeType.IgnoredFolder) {
            // TODO: Consider wording
            // We can usually still search for a file that is not indexed. An ignored file is completely excluded
            presentation.addText(UnityPluginExplorerBundle.message("label.ignored.no.index", SolutionViewPaneBase.TextSeparator),
                                 SimpleTextAttributes.GRAYED_ATTRIBUTES)
        }

        // Add additional info for directories
        val unityExplorer = UnityExplorer.getInstance(myProject)
        if (virtualFile.isDirectory && unityExplorer.showProjectNames) {
            addProjects(presentation)
        }

        // Add tooltip for non-imported files and folders. Also, show the full name if we're hiding the tilde suffix.
        if (isHiddenAsset(virtualFile) || descendentOf == AncestorNodeType.HiddenAsset) {
            var tooltip = if (presentation.tooltip.isNullOrEmpty()) "" else presentation.tooltip + "<br/>"
            if (!SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles && isFolderEndingWithTilde(virtualFile)) {
                tooltip += virtualFile.name + "<br/>"
            }
            presentation.tooltip = tooltip +
                                   if (virtualFile.isDirectory) UnityPluginExplorerBundle.message(
                                       "tooltip.this.folder.not.imported.into.asset.database")
                                   else
                                       UnityPluginExplorerBundle.message("tooltip.this.file.not.imported.into.asset.database")
        }

        if (ignored) {
            val tooltip = if (presentation.tooltip.isNullOrEmpty()) "" else presentation.tooltip + "<br/>"
            presentation.tooltip = tooltip + if (virtualFile.isDirectory)
                UnityPluginExplorerBundle.message("tooltip.this.folder.matches.ignored.file.folders.pattern")
            else
                UnityPluginExplorerBundle.message("tooltip.this.file.matches.ignored.file.folders.pattern")
        }
    }

    override fun getName(): String {
        if (isFolderEndingWithTilde(virtualFile) && !SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            return super.getName().removeSuffix("~")
        }
        return super.getName()
    }

    /*  Special case of {@link #isHiddenAsset(VirtualFile)}
        Files and folders ending with '~' are ignored by the asset importer. Files with '~' are usually backup files,
        so should be hidden. Unity uses folders that end with '~' as a way of distributing files that are not to be
        imported. This is usually `Documentation~` inside packages (https://docs.unity3d.com/Manual/cus-layout.html),
        but it can also be used for distributing code, too (e.g. `Samples~`). This code will not be treated as assets
        by Unity, but will still be added to the generated .csproj files to allow for use as e.g. command line tools
    */
    private fun isFolderEndingWithTilde(file: VirtualFile) = descendentOf != AncestorNodeType.FileSystem && file.isDirectory && file.name.endsWith(
        "~")

    // Ignored by IDE
    private fun isIgnoredFolder(file: VirtualFile) = file.isDirectory && FileTypeManager.getInstance().isFileIgnored(virtualFile)

    protected fun addProjects(presentation: PresentationData) {
        val projectNames = entities   // One node for each project that this directory is part of
            .mapNotNull { containingProjectNode(it) }
            .map(::stripDefaultProjectPrefix)
            .filter { it.isNotEmpty() }
            .sortedWith(String.CASE_INSENSITIVE_ORDER)
            .distinct()
        if (projectNames.any()) {
            val maxProjectsInDescription = 3
            val maxProjectsInTooltip = 10
            var description = projectNames.take(maxProjectsInDescription).joinToString(", ")
            if (projectNames.count() > maxProjectsInDescription) {
                description += ", …"
                presentation.tooltip = UnityPluginExplorerBundle.message("tooltip.contains.files.from.multiple.projects") + "<br/>" +
                                       projectNames.take(maxProjectsInTooltip).joinToString("<br/>") +
                                       if (projectNames.count() > maxProjectsInTooltip) "<br/>" + UnityPluginExplorerBundle.message(
                                           "tooltip.and.count.others", projectNames.count() - maxProjectsInTooltip)
                                       else ""
            }
            presentation.addText(" ($description)", SimpleTextAttributes.GRAYED_ATTRIBUTES)
        }
    }

    private fun stripDefaultProjectPrefix(it: ProjectModelEntity): String {
        // Assembly-CSharp => ""
        // Assembly-CSharp-Editor => Editor
        // Assembly-CSharp.Player => Player
        return it.name.replace(UnityExplorer.DefaultProjectPrefixRegex, "")
    }

    private fun containingProjectNode(entity: ProjectModelEntity): ProjectModelEntity? {
        if (descendentOf == AncestorNodeType.FileSystem) {
            return null
        }

        if (entity.isProject())
            return null

        val projectEntity = entity.containingProjectEntity() ?: return null

        // Show the project on the owner of the assembly definition file
        val dir = entity.url?.virtualFile
        if (dir != null && hasAssemblyDefinitionFile(dir)) {
            return projectEntity
        }

        // Hide the project if we're under an assembly definition - the first .asmdef we meet is the root of this project
        if (isUnderAssemblyDefinition()) {
            return null
        }

        // These special folders aren't used in Packages
        if (descendentOf == AncestorNodeType.Assets) {

            // This won't work if the projects are renamed by some kind of Unity plugin
            // If the project is -Editor, hide if this node is under the Editor folder
            // If the project is -firstpass, hide if this node is under Plugins, Standard Assets or Pro Standard Assets
            // If the project is -Editor-firstpass, see if this node is under an Editor folder that is itself under
            //   Plugins, Standard Assets, Pro Standard Assets
            if (projectEntity.name == UnityExplorer.DefaultProjectPrefix + "-Editor" && isUnderEditorFolder()) {
                return null
            }
            if (projectEntity.name == UnityExplorer.DefaultProjectPrefix + "-firstpass" && isUnderFirstpassFolder()) {
                return null
            }
            if (projectEntity.name == UnityExplorer.DefaultProjectPrefix + "-Editor-firstpass") {
                val editor = findAncestor(this.parent as? FileSystemNodeBase?, "Editor")
                if (editor != null && isUnderFirstpassFolder(editor)) {
                    return null
                }
            }
        }

        return projectEntity
    }

    private fun forEachAncestor(root: FileSystemNodeBase?, action: FileSystemNodeBase.() -> Boolean): FileSystemNodeBase? {
        var node: FileSystemNodeBase? = root
        while (node != null) {
            if (node.action()) {
                return node
            }
            node = node.parent as? FileSystemNodeBase
        }
        return null
    }

    @Suppress("SameParameterValue")
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

    private fun calculateIcon(): Icon {
        if (isIgnoredFolder(virtualFile) || (virtualFile.isDirectory && descendentOf == AncestorNodeType.IgnoredFolder)) {
            return UnityIcons.Explorer.UnloadedFolder
        }

        if (descendentOf != AncestorNodeType.FileSystem) {
            // Under Packages, the only special folder is "Resources". As per Maxime @ Unity:
            // "Resources folders work the same in packages as under Assets, but that's mostly it. Editor folders have no
            // special semantics, Gizmos don't work, there's no StreamingAssets, no Plugins"
            if (name.equals("Resources", true)) {
                return UnityIcons.Explorer.ResourcesFolder
            }

            if (descendentOf == AncestorNodeType.Assets) {
                if (name.equals("Editor", true) && !isUnderAssemblyDefinition()) {
                    return UnityIcons.Explorer.EditorFolder
                }

                if (parent is AssetsRootNode) {
                    val rootSpecialIcon = when (name.lowercase(Locale.getDefault())) {
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

            // Note that its only the root node that's marked as "unloaded"/not imported. Child files and folder icons
            // are rendered as normal
            if (virtualFile.isDirectory && isHiddenAsset(virtualFile)) {
                return UnityIcons.Explorer.UnloadedFolder
            }
        }

        return virtualFile.calculateFileSystemIcon(myProject)
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<NestingNode<VirtualFile>>): FileSystemNodeBase {
        val desc = if (isIgnoredFolder(virtualFile) || (!virtualFile.isDirectory && isIgnoredFolder(virtualFile.parent))) {
            AncestorNodeType.IgnoredFolder
        }
        else if (isHiddenAsset(virtualFile)) {
            AncestorNodeType.HiddenAsset
        }
        else {
            descendentOf
        }
        return UnityExplorerFileSystemNode(myProject, virtualFile, nestedFiles, desc)
    }

    override fun getVirtualFileChildren(): List<VirtualFile> {
        return super.getVirtualFileChildren().filter { shouldShowVirtualFile(it) }
    }

    private fun shouldShowVirtualFile(file: VirtualFile): Boolean {
        if (SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            return true
        }

        // special case, this check should go before isPartOfAssetDataBase
        if (isFolderEndingWithTilde(file)) {
            return UnityExplorer.getInstance(myProject).showTildeFolders
        }

        return !isHiddenAsset(file)
    }

    override fun getFileStatus(): FileStatus {
        // Read only package files are cached in the Library folder, which is ignored by VCS, but it's pointless showing
        // these as IGNORED. We need to get the ultimate ancestor of this node, so we don't mark hidden or ignored files
        // inside read only packages as IGNORED.
        return if (isDescendantOfReadOnlyPackage(this)) {
            FileStatus.NOT_CHANGED
        }
        else {
            super.getFileStatus()
        }
    }

    override fun getFileStatusColor(status: FileStatus?): Color? {
        // The super implementation will try to recursively discover a more appropriate status if asked for NOT_CHANGED,
        // so it can propagate changes from descendants. Make sure that we always show NOT_CHANGED for readonly packages
        return if (isDescendantOfReadOnlyPackage(this) && status == FileStatus.NOT_CHANGED) {
            status?.color
        }
        else {
            super.getFileStatusColor(status)
        }
    }

    override fun getTestPresentation(): String {
        if (this.presentation.tooltip != null)
            return "${super.getTestPresentation()}, tooltip=${this.presentation.tooltip}"
        return super.getTestPresentation()
    }
}
