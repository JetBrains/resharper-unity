package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.plugins.unity.packageManager.PackageData
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageSource
import com.jetbrains.rider.projectView.views.*
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane
import icons.UnityIcons

class PackagesRootNode(project: Project, packagesFolder: VirtualFile)
    : UnityExplorerFileSystemNode(project, packagesFolder, emptyList(), AncestorNodeType.FileSystem) {

    private val packageManager = PackageManager.getInstance(myProject)

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.presentableText = "Packages"
        presentation.setIcon(UnityIcons.Explorer.PackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {

        // Add file system children, which will include embedded packages
        val children = super.calculateChildren()

        val allPackages = packageManager.getPackages()

        // Add the "Read Only" node for modules and referenced packages. Don't add the node if we haven't loaded
        // packages yet
        if (allPackages.any { it.source.isReadOnly() }) {
            children.add(0, ReadOnlyPackagesRootNode(myProject, packageManager))
        }

        // Also include any local (file: based) packages, plus all unresolved packages
        allPackages.filter { it.source == PackageSource.Local }.forEach { addPackageNode(children, it) }
        allPackages.filter { it.source == PackageSource.Unknown }.forEach { addPackageNode(children, it) }

        return children
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<NestingNode<VirtualFile>>): FileSystemNodeBase {
        // If the child folder is an embedded package, add it as a package node
        if (virtualFile.isDirectory) {
            packageManager.tryGetPackage(virtualFile)?.let {
                return PackageNode(myProject, packageManager, virtualFile, it)
            }
        }
        return super.createNode(virtualFile, nestedFiles)
    }

    // Required for "Locate in Solution Explorer". Treat as "can contain". Returning false stops the visitor. If we
    // return true, which is "maybe", then the child nodes are expanded as the visitor keeps looking
    override fun contains(file: VirtualFile): Boolean {
        return children.any { (it as? SolutionViewNode)?.contains(file) == true }
    }

    private fun addPackageNode(children: MutableList<AbstractTreeNode<*>>, thePackage: PackageData) {
        if (thePackage.packageFolder != null) {
            children.add(PackageNode(myProject, packageManager, thePackage.packageFolder, thePackage))
        }
        else {
            children.add(UnknownPackageNode(myProject, thePackage))
        }
    }
}

class PackageNode(project: Project, private val packageManager: PackageManager, packageFolder: VirtualFile, private val packageData: PackageData)
    : UnityExplorerFileSystemNode(project, packageFolder, emptyList(), AncestorNodeType.fromPackageData(packageData)), Comparable<AbstractTreeNode<*>> {

    init {
        icon = when (packageData.source) {
            PackageSource.Registry -> UnityIcons.Explorer.ReferencedPackage
            PackageSource.Embedded -> UnityIcons.Explorer.EmbeddedPackage
            PackageSource.Local -> UnityIcons.Explorer.LocalPackage
            PackageSource.LocalTarball -> UnityIcons.Explorer.LocalTarballPackage
            PackageSource.BuiltIn -> UnityIcons.Explorer.BuiltInPackage
            PackageSource.Git -> UnityIcons.Explorer.GitPackage
            PackageSource.Unknown -> UnityIcons.Explorer.UnknownPackage
        }
    }

    override fun getName() = packageData.displayName

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(icon)
        presentation.addNonIndexedMark(myProject, virtualFile)

        // Note that this might also set the tooltip if we have too many projects underneath
        if (UnityExplorer.getInstance(myProject).showProjectNames) {
            addProjects(presentation)
        }

        val existingTooltip = presentation.tooltip ?: ""

        var tooltip = "<html>" + getPackageTooltip(name, packageData)
        tooltip += when (packageData.source) {
            PackageSource.Embedded -> if (virtualFile.name != name) "<br/><br/>Folder name: ${virtualFile.name}" else ""
            PackageSource.Local -> "<br/><br/>Folder location: ${virtualFile.path}"
            PackageSource.LocalTarball -> "<br/><br/>Tarball location: ${packageData.tarballLocation}"
            PackageSource.Git -> {
                var text = "<br/><br/>"
                text += if (!packageData.gitUrl.isNullOrEmpty()) {
                    "Git URL: ${packageData.gitUrl}"
                } else {
                    "Unknown Git URL"
                }
                if (!packageData.gitHash.isNullOrEmpty()) {
                    text += "<br/>Hash: ${packageData.gitHash}"
                }
                if (!packageData.gitRevision.isNullOrEmpty()) {
                    text += "<br/>Revision: ${packageData.gitRevision}"
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

        if (packageData.dependencies.isNotEmpty()) {
            children.add(0, PackageDependenciesRoot(myProject, packageManager, packageData))
        }

        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        // Compare by display name, rather than the default file name
        return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
    }
}

class PackageDependenciesRoot(project: Project, private val packageManager: PackageManager, private val packageData: PackageData)
    : SolutionViewNode<Any>(project, packageData) {

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Dependencies"
        presentation.setIcon(UnityIcons.Explorer.DependenciesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for ((name, version) in packageData.dependencies) {
            children.add(PackageDependencyItemNode(myProject, packageManager, name, version))
        }
        return children
    }

    override fun contains(file: VirtualFile) = false
}

class PackageDependencyItemNode(project: Project, private val packageManager: PackageManager, private val packageId: String, version: String)
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

    override fun canNavigate() = packageManager.tryGetPackage(packageId)?.packageFolder != null
    override fun navigate(requestFocus: Boolean) {
        val packageFolder = packageManager.tryGetPackage(packageId)?.packageFolder ?: return
        myProject.navigateToSolutionView(packageFolder, requestFocus)
    }
}

class ReadOnlyPackagesRootNode(project: Project, private val packageManager: PackageManager)
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
        children.add(BuiltinPackagesRootNode(myProject, packageManager))

        for (packageData in packageManager.getPackages().filter { it.source.isReadOnly() && it.source != PackageSource.BuiltIn }) {
            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(myProject, packageData))
            }
            else {
                children.add(PackageNode(myProject, packageManager, packageData.packageFolder, packageData))
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

class BuiltinPackagesRootNode(project: Project, private val packageManager: PackageManager)
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
        for (packageData in packageManager.getPackages().filter { it.source == PackageSource.BuiltIn }) {

            // All modules should have a package folder, or it means we haven't been able to resolve it
            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(myProject, packageData))
            }
            else {
                children.add(BuiltinPackageNode(myProject, packageData))
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
class BuiltinPackageNode(project: Project, private val packageData: PackageData)
    : UnityExplorerFileSystemNode(project, packageData.packageFolder!!, emptyList(), AncestorNodeType.ReadOnlyPackage), Comparable<AbstractTreeNode<*>> {

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

    override fun getName() = packageData.displayName

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.BuiltInPackage)
        if (SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles) {
            presentation.addNonIndexedMark(myProject, virtualFile)
        }

        val tooltip = getPackageTooltip(name, packageData)
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
class UnknownPackageNode(project: Project, private val packageData: PackageData)
    : AbstractTreeNode<Any>(project, packageData) {

    init {
        icon = when (packageData.source) {
            PackageSource.BuiltIn -> UnityIcons.Explorer.BuiltInPackage
            else -> UnityIcons.Explorer.UnknownPackage
        }
    }

    override fun getName() = packageData.displayName
    override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(icon)

        // Description can be an error message
        if (packageData.description?.isNotEmpty() == true) {
            presentation.tooltip = formatDescription(packageData.description)
        }
    }
}

private fun getPackageTooltip(displayName: String, packageData: PackageData): String {
    var tooltip = displayName
    if (packageData.version.isNotEmpty()) {
        tooltip += " ${packageData.version}"
    }
    if (displayName != packageData.id) {
        tooltip += "<br/>${packageData.id}"
    }
    if (packageData.description?.isNotEmpty() == true) {
        tooltip += "<br/><br/>" + formatDescription(packageData.description)
    }
    return tooltip
}

private fun formatDescription(description: String): String {
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
