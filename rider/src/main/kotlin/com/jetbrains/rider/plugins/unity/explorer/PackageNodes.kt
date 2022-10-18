@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsContexts
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource
import com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity
import com.jetbrains.rider.plugins.unity.workspace.getPackages
import com.jetbrains.rider.plugins.unity.workspace.tryGetPackage
import com.jetbrains.rider.projectView.views.*
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane
import icons.UnityIcons

class PackagesRootNode(project: Project, packagesFolder: VirtualFile)
    : UnityExplorerFileSystemNode(project, packagesFolder, emptyList(), AncestorNodeType.FileSystem) {

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.presentableText = "Packages"
        presentation.setIcon(UnityIcons.Explorer.PackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {

        // Add file system children, which will include embedded packages
        val children = super.calculateChildren()

        val allPackages = WorkspaceModel.getInstance(myProject).getPackages()

        // Add the "Read Only" node for modules and referenced packages. Don't add the node if we haven't loaded
        // packages yet
        if (allPackages.any { it.isReadOnly() }) {
            children.add(0, ReadOnlyPackagesRootNode(myProject))
        }

        // Also include any local (file: based) packages, plus all unresolved packages
        allPackages.filter { it.source == UnityPackageSource.Local }.forEach { addPackageNode(children, it) }
        allPackages.filter { it.source == UnityPackageSource.Unknown }.forEach { addPackageNode(children, it) }

        return children
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<NestingNode<VirtualFile>>): FileSystemNodeBase {
        // If the child folder is an embedded package, add it as a package node
        if (virtualFile.isDirectory) {
            WorkspaceModel.getInstance(myProject).tryGetPackage(virtualFile)?.let {
                return PackageNode(myProject, virtualFile, it)
            }
        }
        return super.createNode(virtualFile, nestedFiles)
    }

    // Required for "Locate in Solution Explorer". Treat as "can contain". Returning false stops the visitor. If we
    // return true, which is "maybe", then the child nodes are expanded as the visitor keeps looking
    override fun contains(file: VirtualFile): Boolean {
        return children.any { (it as? SolutionViewNode)?.contains(file) == true }
    }

    private fun addPackageNode(children: MutableList<AbstractTreeNode<*>>, packageEntity: UnityPackageEntity) {
        val packageFolder = packageEntity.packageFolder
        if (packageFolder != null) {
            children.add(PackageNode(myProject, packageFolder, packageEntity))
        }
        else {
            children.add(UnknownPackageNode(myProject, packageEntity))
        }
    }
}

class PackageNode(project: Project, packageFolder: VirtualFile, private val packageEntity: UnityPackageEntity)
    : UnityExplorerFileSystemNode(project, packageFolder, emptyList(), AncestorNodeType.fromPackageData(packageEntity)), Comparable<AbstractTreeNode<*>> {

    init {
        icon = when (packageEntity.source) {
            UnityPackageSource.Registry -> UnityIcons.Explorer.ReferencedPackage
            UnityPackageSource.Embedded -> UnityIcons.Explorer.EmbeddedPackage
            UnityPackageSource.Local -> UnityIcons.Explorer.LocalPackage
            UnityPackageSource.LocalTarball -> UnityIcons.Explorer.LocalTarballPackage
            UnityPackageSource.BuiltIn -> UnityIcons.Explorer.BuiltInPackage
            UnityPackageSource.Git -> UnityIcons.Explorer.GitPackage
            UnityPackageSource.Unknown -> UnityIcons.Explorer.UnknownPackage
        }
    }

    override fun getName() = packageEntity.displayName

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(icon)
        presentation.addNonIndexedMark(myProject, virtualFile)

        // Note that this might also set the tooltip if we have too many projects underneath
        if (UnityExplorer.getInstance(myProject).showProjectNames) {
            addProjects(presentation)
        }

        val existingTooltip = presentation.tooltip ?: ""

        var tooltip = "<html>" + getPackageTooltip(name, packageEntity)
        tooltip += when (packageEntity.source) {
            UnityPackageSource.Embedded -> if (virtualFile.name != name) {UnityBundle.message("folder.name", "<br/><br/>", virtualFile.name)} else ""
            UnityPackageSource.Local -> UnityBundle.message("folder.location", "<br/><br/>", virtualFile.path)
            UnityPackageSource.LocalTarball -> UnityBundle.message("tarball.location", "<br/><br/>", packageEntity.tarballLocation ?: "")
            UnityPackageSource.Git -> {
                var text = "<br/><br/>"
                text += if (!packageEntity.gitUrl.isNullOrEmpty()) {
                    "Git URL: ${packageEntity.gitUrl}"
                } else {
                    "Unknown Git URL"
                }
                if (!packageEntity.gitHash.isNullOrEmpty()) {
                    text += "<br/>Hash: ${packageEntity.gitHash}"
                }
                if (!packageEntity.gitRevision.isNullOrEmpty()) {
                    text += "<br/>Revision: ${packageEntity.gitRevision}"
                }
                text
            }
            else -> ""
        }
        if (existingTooltip.isNotEmpty()) {
            tooltip += "<br/><br/>$existingTooltip"
        }
        tooltip += "</html>"
        presentation.tooltip = tooltip
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = super.calculateChildren()

        if (packageEntity.dependencies.isNotEmpty()) {
            children.add(0, PackageDependenciesRoot(myProject, packageEntity))
        }

        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        // Compare by display name, rather than the default file name
        return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
    }
}

class PackageDependenciesRoot(project: Project, private val packageEntity: UnityPackageEntity)
    : SolutionViewNode<Any>(project, packageEntity) {

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Dependencies"
        presentation.setIcon(UnityIcons.Explorer.DependenciesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for ((name, version) in packageEntity.dependencies) {
            children.add(PackageDependencyItemNode(myProject, name, version))
        }
        return children
    }

    override fun contains(file: VirtualFile) = false
}

class PackageDependencyItemNode(project: Project, private val packageId: String, version: String)
    : SolutionViewNode<Any>(project, "$packageId@$version") {

    init {
        myName = "$packageId@$version"
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> = mutableListOf()
    override fun isAlwaysLeaf() = true
    override fun contains(file: VirtualFile) = false

    override fun update(presentation: PresentationData) {
        presentation.presentableText = name
        presentation.setIcon(UnityIcons.Explorer.PackageDependency)
    }

    override fun canNavigate() = WorkspaceModel.getInstance(myProject).tryGetPackage(packageId)?.packageFolder != null
    override fun navigate(requestFocus: Boolean) {
        val packageFolder = WorkspaceModel.getInstance(myProject).tryGetPackage(packageId)?.packageFolder ?: return
        myProject.navigateToSolutionView(packageFolder, requestFocus)
    }
}

class ReadOnlyPackagesRootNode(project: Project)
    : SolutionViewNode<Any>(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Read only"
        presentation.setIcon(UnityIcons.Explorer.ReadOnlyPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()

        // If we have any packages, we'll have modules
        children.add(BuiltinPackagesRootNode(myProject))

        for (packageEntity in WorkspaceModel.getInstance(myProject).getPackages().filter { it.isReadOnly() && it.source != UnityPackageSource.BuiltIn }) {
            val packageFolder = packageEntity.packageFolder
            if (packageFolder == null) {
                children.add(UnknownPackageNode(myProject, packageEntity))
            }
            else {
                children.add(PackageNode(myProject, packageFolder, packageEntity))
            }
        }
        return children
    }

    // Treat as "can contain". Returning false stops the visitor. If we return true, which is "maybe", then the child
    // nodes are expanded as the visitor keeps looking
    override fun contains(file: VirtualFile): Boolean {
        return children.any { (it as? SolutionViewNode)?.contains(file) == true }
    }
}

class BuiltinPackagesRootNode(project: Project)
    : SolutionViewNode<Any>(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Modules"
        presentation.setIcon(UnityIcons.Explorer.BuiltInPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for (packageEntity in WorkspaceModel.getInstance(myProject).getPackages().filter { it.source == UnityPackageSource.BuiltIn }) {

            // All modules should have a package folder, or it means we haven't been able to resolve it
            if (packageEntity.packageFolder == null) {
                children.add(UnknownPackageNode(myProject, packageEntity))
            }
            else {
                children.add(BuiltinPackageNode(myProject, packageEntity))
            }
        }
        return children
    }

    // Required for "Locate in Solution Explorer". Treat as "can contain". Returning false stops the visitor. If we
    // return true, which is "maybe", then the child nodes are expanded as the visitor keeps looking
    override fun contains(file: VirtualFile): Boolean {
        return children.any { (it as? SolutionViewNode)?.contains(file) == true }
    }
}

// Represents a module, built in part of the Unity product. We show it as a single node with no children, unless we have
// "show hidden items" enabled, in which case we show the package folder, including the package.json.
// Note that a module can have dependencies. Perhaps we want to always show this as a folder, including the Dependencies
// node?
class BuiltinPackageNode(project: Project, private val packageEntity: UnityPackageEntity)
    : UnityExplorerFileSystemNode(project, packageEntity.packageFolder!!, emptyList(), AncestorNodeType.ReadOnlyPackage), Comparable<AbstractTreeNode<*>> {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {

        if (SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            return super.calculateChildren()
        }

        // Show children if there's anything interesting to show. If it's just package.json or .icon.png, or their
        // meta files, pretend there's no children. We'll show them when show hidden items is enabled
        val children = super.calculateChildren()
        if (children.all { it.name?.startsWith("package.json") == true
                || it.name?.startsWith(".icon.png") == true
                || it.name?.startsWith("package.ModuleCompilationTrigger") == true }) {
            return mutableListOf()
        }
        return super.calculateChildren()
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<NestingNode<VirtualFile>>): FileSystemNodeBase {
        return UnityExplorerFileSystemNode(myProject, virtualFile, nestedFiles, descendentOf)
    }

    override fun canNavigateToSource(): Boolean {
        if (SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            return super.canNavigateToSource()
        }
        return true
    }

    override fun navigate(requestFocus: Boolean) {
        if (SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            return super.navigate(requestFocus)
        }

        val packageJson = virtualFile.findChild("package.json")
        if (packageJson != null) {
            OpenFileDescriptor(myProject, packageJson).navigate(requestFocus)
        }
    }

    override fun getName() = packageEntity.displayName

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.BuiltInPackage)
        if (SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            presentation.addNonIndexedMark(myProject, virtualFile)
        }

        // TODO #Localization RIDER-82737
        val tooltip = getPackageTooltip(name, packageEntity)
        if (tooltip != name) {
            presentation.tooltip = tooltip
        }
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        // Compare by display name, rather than the default file name
        return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
    }
}

// Note that this might get a PackageData with source == PackageSource.BuiltIn
class UnknownPackageNode(project: Project, private val packageEntity: UnityPackageEntity)
    : AbstractTreeNode<Any>(project, packageEntity) {

    init {
        icon = when (packageEntity.source) {
            UnityPackageSource.BuiltIn -> UnityIcons.Explorer.BuiltInPackage
            else -> UnityIcons.Explorer.UnknownPackage
        }
    }

    override fun getName() = packageEntity.displayName
    override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(icon)

        // Description can be an error message
        val description = packageEntity.description
        if (description?.isNotEmpty() == true) {
            presentation.tooltip = formatDescription(description)
        }
    }
}

@NlsSafe
private fun getPackageTooltip(displayName: String, packageEntity: UnityPackageEntity): String {
    var tooltip = displayName
    if (packageEntity.version.isNotEmpty()) {
        tooltip += " ${packageEntity.version}"
    }
    if (displayName != packageEntity.packageId) {
        tooltip += "<br/>${packageEntity.packageId}"
    }
    val description = packageEntity.description
    if (description?.isNotEmpty() == true) {
        tooltip += "<br/><br/>" + formatDescription(description)
    }
    return tooltip
}

@NlsContexts.Tooltip
private fun formatDescription(@NlsSafe description: String): String {
    val text = description.replace("\n", "<br/>").let {
        StringUtil.shortenTextWithEllipsis(it, 600, 0, true)
    }

    // Very crude. This should really be measured by font + pixels, not characters.
    // Hopefully we can replace all of this with QuickDoc though, which has smarter wrapping.
    val shouldWrap = StringUtil.splitByLines(text).any { it.length > 50 }
    return if (shouldWrap) {
        "<div width=\"500\">$text</div>"
    }
    else {
        text
    }
}
