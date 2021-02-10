package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.plugins.unity.packageManager.PackageData
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageSource
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.views.*
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane
import icons.UnityIcons

class PackagesRootNode(project: Project, private val packageManager: PackageManager)
    : UnityExplorerFileSystemNode(project, packageManager.packagesFolder, emptyList(), AncestorNodeType.FileSystem) {

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.presentableText = "Packages"
        presentation.setIcon(UnityIcons.Explorer.PackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {

        // Add file system children, which will include embedded packages
        val children = super.calculateChildren()

        // Also include any local (file: based) packages, plus all unresolved packages
        packageManager.localPackages.forEach { addPackageNode(children, it) }
        packageManager.unknownPackages.forEach { addPackageNode(children, it) }

        // Add a root node for modules and referenced packages
        if (packageManager.immutablePackages.any()) {
            children.add(0, ReadOnlyPackagesRootNode(myProject, packageManager))
        }

        return children
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<NestingNode<VirtualFile>>): FileSystemNodeBase {
        // If the child folder is an embedded package, add it as a package node
        if (virtualFile.isDirectory) {
            packageManager.getPackageData(virtualFile)?.let {
                val embeddedPackageData = PackageData(it.name, virtualFile, it.details, PackageSource.Embedded)
                return PackageNode(myProject, packageManager, virtualFile, embeddedPackageData)
            }
        }
        return super.createNode(virtualFile, nestedFiles)
    }

    // Required for "Locate in Solution Explorer" to work. If we return false, the solution view visitor stops walking.
    // True is effectively "maybe"
    override fun contains(file: VirtualFile) = true

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

    override fun getName() = packageData.details.displayName

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
                var text = "<br/><br/>Git URL: ${packageData.gitDetails?.url}"
                if (!packageData.gitDetails?.hash.isNullOrEmpty()) {
                    text += "<br/>Hash: ${packageData.gitDetails?.hash}"
                }
                if (!packageData.gitDetails?.revision.isNullOrEmpty()) {
                    text += "<br/>Revision: ${packageData.gitDetails?.revision}"
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

        if (packageData.details.dependencies.isNotEmpty()) {
            children.add(0, PackageDependenciesRoot(myProject, packageManager, packageData))
        }

        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        // Compare by name, rather than ID
        return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
    }
}

class PackageDependenciesRoot(project: Project, private val packageManager: PackageManager, private val packageData: PackageData)
    : AbstractTreeNode<Any>(project, packageData) {

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Dependencies"
        presentation.setIcon(UnityIcons.Explorer.DependenciesRoot)
    }

    override fun getChildren(): MutableCollection<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for ((name, version) in packageData.details.dependencies) {
            children.add(PackageDependencyItemNode(myProject, packageManager, name, version))
        }
        return children
    }
}

class PackageDependencyItemNode(project: Project, private val packageManager: PackageManager, private val packageName: String, version: String)
    : AbstractTreeNode<Any>(project, "$packageName@$version") {

    init {
        myName = "$packageName@$version"
    }

    override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.presentableText = name
        presentation.setIcon(UnityIcons.Explorer.PackageDependency)
    }

    override fun canNavigate(): Boolean {
        return packageManager.getPackageData(packageName) != null
    }

    override fun navigate(requestFocus: Boolean) {
        val packageData = packageManager.getPackageData(packageName)
        if (packageData?.packageFolder == null) return
        myProject.navigateToSolutionView(packageData.packageFolder, requestFocus)
    }
}

abstract class FolderContainerNodeBase(project: Project, key: Any)
    : SolutionViewNode<Any>(project, key) {

    private val childFolders = mutableSetOf<VirtualFile>()

    protected fun addChildFolder(packageFolder: VirtualFile) {
        childFolders.add(packageFolder)
    }

    // Note that this requires the children to have been expanded first. The SolutionViewVisitor will ensure this happens
    override fun contains(file: VirtualFile): Boolean {
        if (childFolders.contains(file)) return true
        for (packageFolder in childFolders) {
            if (VfsUtil.isAncestor(packageFolder,  file, false)) return true
        }
        return false
    }
}

class ReadOnlyPackagesRootNode(project: Project, private val packageManager: PackageManager)
    : FolderContainerNodeBase(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Read only"
        presentation.setIcon(UnityIcons.Explorer.ReadOnlyPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()

        if (packageManager.hasBuiltInPackages) {
            children.add(BuiltinPackagesRootNode(myProject, packageManager))

            // Add the builtin packages root folder to the list of folders we know are under this node. This lets us
            // correctly handle `contains()` for built in packages, which in turn means we can navigate to a built in
            // package by folder (e.g. by double clicking a dependency node)
            try {
                UnityInstallationFinder.getInstance(myProject).getBuiltInPackagesRoot()?.let { path ->
                    VfsUtil.findFile(path, true)?.let {
                        addChildFolder(it)
                    }
                }
            } catch (throwable: Throwable) {
                // Do nothing. It just means navigation to built in packages from dependency nodes won't work
            }
        }

        for (packageData in packageManager.immutablePackages) {
            if (packageData.source == PackageSource.BuiltIn) continue

            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(myProject, packageData))
            }
            else {
                children.add(PackageNode(myProject, packageManager, packageData.packageFolder, packageData))
                addChildFolder(packageData.packageFolder)
            }
        }
        return children
    }
}

class BuiltinPackagesRootNode(project: Project, private val packageManager: PackageManager)
    : FolderContainerNodeBase(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Modules"
        presentation.setIcon(UnityIcons.Explorer.BuiltInPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for (packageData in packageManager.immutablePackages) {
            if (packageData.source != PackageSource.BuiltIn) continue

            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(myProject, packageData))
            }
            else {
                children.add(BuiltinPackageNode(myProject, packageData))
                addChildFolder(packageData.packageFolder)
            }
        }
        return children
    }
}

// Represents a module, built in part of the Unity product. We show it as a single node with no children, unless we have
// "show hidden items" enabled, in which case we show the package folder, including the package.json.
// Note that a module can have dependencies. Perhaps we want to always show this as a folder, including the Dependencies
// node?
class BuiltinPackageNode(project: Project, private val packageData: PackageData)
    : UnityExplorerFileSystemNode(project, packageData.packageFolder!!, emptyList(), AncestorNodeType.ReadOnlyPackage) {

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

    override fun getName() = packageData.details.displayName

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

    override fun getName() = packageData.details.displayName
    override fun getChildren(): MutableCollection<out AbstractTreeNode<Any>> = arrayListOf()
    override fun isAlwaysLeaf() = true

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(icon)

        // Description can be an error message
        if (packageData.details.description.isNotEmpty()) {
            presentation.tooltip = formatDescription(packageData)
        }
    }
}

private fun getPackageTooltip(name: String, packageData: PackageData): String {
    var tooltip = name
    if (packageData.details.version.isNotEmpty()) {
        tooltip += " ${packageData.details.version}"
    }
    if (name != packageData.details.canonicalName) {
        tooltip += "<br/>${packageData.details.canonicalName}"
    }
    if (packageData.details.author.isNotEmpty()) {
        tooltip += "<br/>${packageData.details.author}"
    }
    if (packageData.details.description.isNotEmpty()) {
        tooltip += "<br/><br/>" + formatDescription(packageData)
    }
    return tooltip
}

private fun formatDescription(packageData: PackageData): String {
    val description =  packageData.details.description.replace("\n", "<br/>").let {
        StringUtil.shortenTextWithEllipsis(it, 600, 0, true)
    }

    // Very crude. This should really be measured by font + pixels, not characters.
    // Hopefully we can replace all of this with QuickDoc though, which has smarter wrapping.
    val shouldWrap = StringUtil.splitByLines(packageData.details.description).any { it.length > 50 }
    return if (shouldWrap) {
        "<div width=\"500\">$description</div>"
    }
    else {
        description
    }
}
