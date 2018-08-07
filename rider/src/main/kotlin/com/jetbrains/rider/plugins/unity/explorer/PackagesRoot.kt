package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.views.FileSystemNodeBase
import com.jetbrains.rider.projectView.views.SolutionViewNode
import com.jetbrains.rider.projectView.views.addNonIndexedMark
import com.jetbrains.rider.projectView.views.fileSystemExplorer.FileSystemExplorerNode
import com.jetbrains.rider.projectView.views.navigateToSolutionView

// Packages are included in a project by listing in the "dependencies" node of Packages/manifest.json. Packages can
// contain assets, such as source, resources, and .dlls. They can also include .asmdef files
// a) Built-in (modules). These are pseudo-package that ship built in to Unity. They can be resolved by manifests in the
//    application directory. Modules are cached in an application specific folder, so can't be found without knowing the
//    path to the currently running instance of Unity. Path is: Editor/Data/Resources/PackageManager/BuiltInPackages for
//    Windows and Unity.app/Contents/Resources/PackageManager/BuiltInPackages on OSX
// b) Referenced. These are cached in a per-user and per-registry location, and are read only. Any .asmdef files in the
//    package will be used to compile an assembly, saved to Library/ScriptAssemblies and added as a binary reference to
//    the project. A referenced package can be copied into the Packages folder to convert it into a writable embedded
//    package. Cache folder is %LOCALAPPDATA%\Unity\cache\packages on Windows and ~/Library/Unity/cache/packages on OSX
// c) Embedded. This is a package that lives inside the Packages folder, and is read-write. Any .asmdef files are used
//    to create C# projects and added to the generated solution.
// d) Local. These are read-write packages that live outside of the Packages folder. Any .asmdef files are used to
//    generate C# projects. The version in Packages/manifest.json begins with `file:` and is a path to the package,
//    either relative to the project root, or fully qualified
// e) Git. Currently undocumented. Unity will check out a git repo to a cache folder and treat it as a read-only package
// f) Excluded. A package can have a version of "excluded" in the manifest.json. It is simply ignored
//
// If there is a Packages/manifest.json file, show the Packages node in the Unity Explorer. The Packages node will show
// all editable packages at the root, with a child node for read only packages. It will show all files and folders under
// the Packages folder, with embedded packages being highlighted and sorted to the top. Local packages will also be
// listed here. All other packages will be listed under "Read only", with source packages listed at the top as folders,
// followed by modules and other/unresolved packages
//
// Potential actions:
// a) If a folder has a package.json, but isn't listed in manifest.json, highlight with an error, and offer a right
//    click action to add to manifest.json
// b) Right click on a referenced package to convert to embedded - simply copy into the project's Packages folder

class PackagesRoot(project: Project, private val packagesManager: PackagesManager)
    : UnityExplorerNode(project, packagesManager.packagesFolder, listOf()) {

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.presentableText = "Packages"
        presentation.setIcon(UnityIcons.Explorer.PackagesRoot)
    }

    override fun isAlwaysExpand() = true

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = super.calculateChildren()

        // We want the children to be file system folders and editable packages, which means embedded packages and local
        // packages. We've already added the embedded packages by including file system folders
        val localPackages = packagesManager.localPackages
        for (localPackage in localPackages) {
            if (localPackage.packageFolder != null) {
                children.add(PackageNode(project!!, packagesManager, localPackage.packageFolder, localPackage))
            }
            else {
                children.add(UnknownPackageNode(project!!, localPackage))
            }
        }

        if (packagesManager.immutablePackages.any())
            children.add(0, ReadOnlyPackagesRoot(project!!, packagesManager))

        return children
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        if (virtualFile.isDirectory) {
            packagesManager.getPackageData(virtualFile)?.let {
                val embeddedPackageData = PackageData(it.name, virtualFile, it.details, PackageSource.Embedded)
                return PackageNode(project!!, packagesManager, virtualFile, embeddedPackageData)
            }
        }
        return super.createNode(virtualFile, nestedFiles)
    }

    // Required for "Locate in Solution Explorer" to work. If we return false, the solution view visitor stops walking.
    // True is effectively "maybe"
    override fun contains(file: VirtualFile) = true
}

class PackageNode(project: Project, private val packagesManager: PackagesManager, packageFolder: VirtualFile, private val packageData: PackageData)
    : UnityExplorerNode(project, packageFolder, listOf()), Comparable<AbstractTreeNode<*>> {

    init {
        icon = when (packageData.source) {
            PackageSource.Registry -> UnityIcons.Explorer.ReferencedPackage
            PackageSource.Embedded -> UnityIcons.Explorer.EmbeddedPackage
            PackageSource.Local -> UnityIcons.Explorer.LocalPackage
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
        addProjects(presentation)

        // Richer tooltip
        val existingTooltip = presentation.tooltip ?: ""
        var newTooltip = ""
        if (name != virtualFile.name) {
            newTooltip += virtualFile.name + "\n"
        }
        if (name != packageData.details.canonicalName) {
            newTooltip += packageData.details.canonicalName + "\n"
        }
        if (!packageData.details.version.isEmpty()) {
            newTooltip += packageData.details.version
        }
        if (!packageData.details.description.isEmpty()) {
            newTooltip += "\n${packageData.details.description}"
        }
        if (existingTooltip.isNotEmpty()) {
            newTooltip += "\n" + existingTooltip
        }
        presentation.tooltip = newTooltip
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = super.calculateChildren()

        if (!packageData.details.dependencies.isEmpty()) {
            children.add(0, DependenciesRoot(project!!, packagesManager, packageData))
        }

        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        if (other is PackageNode) {
            return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
        }
        else if (other is ReadOnlyPackagesRoot || other is BuiltinPackagesRoot) {
            return 1
        }
        // Other is UnresolvedPackageNode
        return -1
    }
}

class DependenciesRoot(project: Project, private val packagesManager: PackagesManager, private val packageData: PackageData)
    : AbstractTreeNode<Any>(project, packageData), Comparable<AbstractTreeNode<*>> {

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Dependencies"
        presentation.setIcon(UnityIcons.Explorer.DependenciesRoot)
    }

    override fun getChildren(): MutableCollection<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for ((name, version) in packageData.details.dependencies) {
            children.add(DependencyItemNode(project!!, packagesManager, name, version))
        }
        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>) = -1
}

class DependencyItemNode(project: Project, private val packagesManager: PackagesManager, private val packageName: String, version: String)
    : AbstractTreeNode<Any>(project, "$packageName@$version"), Comparable<AbstractTreeNode<*>> {

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
        return packagesManager.getPackageData(packageName) != null
    }

    override fun navigate(requestFocus: Boolean) {
        val packageData = packagesManager.getPackageData(packageName)
        if (packageData?.packageFolder == null) return
        project!!.navigateToSolutionView(packageData.packageFolder, requestFocus)
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        return String.CASE_INSENSITIVE_ORDER.compare(this.name, other.name)
    }
}

abstract class CompositeFolderRoot(project: Project, key: Any)
    : SolutionViewNode<Any>(project, key) {

    private val packageFolders = mutableSetOf<VirtualFile>()

    protected fun addPackageFolder(packageFolder: VirtualFile) {
        packageFolders.add(packageFolder)
    }

    // Note that this requires the children to have been expanded first. The SolutionViewVisitor will ensure this happens
    override fun contains(file: VirtualFile): Boolean {
        if (packageFolders.contains(file)) return true
        for (packageFolder in packageFolders) {
            if (VfsUtil.isAncestor(packageFolder,  file, false)) return true
        }
        return false
    }
}

class ReadOnlyPackagesRoot(project: Project, private val packagesManager: PackagesManager)
    : CompositeFolderRoot(project, key), Comparable<AbstractTreeNode<*>> {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Read only"
        presentation.setIcon(UnityIcons.Explorer.ReadOnlyPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()

        if (packagesManager.hasBuiltInPackages)
            children.add(BuiltinPackagesRoot(project!!, packagesManager))

        for (packageData in packagesManager.immutablePackages) {
            if (packageData.source == PackageSource.BuiltIn) continue

            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(project!!, packageData))
            }
            else {
                children.add(PackageNode(project!!, packagesManager, packageData.packageFolder, packageData))
                addPackageFolder(packageData.packageFolder)
            }
        }
        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>) = -1
}

class BuiltinPackagesRoot(project: Project, private val packagesManager: PackagesManager)
    : CompositeFolderRoot(project, key), Comparable<AbstractTreeNode<*>> {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Modules"
        presentation.setIcon(UnityIcons.Explorer.BuiltInPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for (packageData in packagesManager.immutablePackages) {
            if (packageData.source != PackageSource.BuiltIn) continue

            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(project!!, packageData))
            }
            else {
                children.add(BuiltinPackageNode(project!!, packageData))
                addPackageFolder(packageData.packageFolder)
            }
        }
        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>) = -1
}

// Represents a module, built in part of the Unity product. We show it as a single node with no children, unless we have
// "show hidden items" enabled, in which case we show the package folder, including the package.json.
// Note that a module can have dependencies. Perhaps we want to always show this as a folder, including the Dependencies
// node?
class BuiltinPackageNode(project: Project, private val packageData: PackageData)
    : FileSystemNodeBase(project, packageData.packageFolder!!, listOf()), Comparable<AbstractTreeNode<*>> {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {

        if (!UnityExplorer.getInstance(project!!).myShowHiddenItems) {
            return arrayListOf()
        }
        return super.calculateChildren()
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        return FileSystemExplorerNode(project!!, virtualFile, nestedFiles, false)
    }

    override fun canNavigateToSource(): Boolean {
        if (UnityExplorer.getInstance(project!!).myShowHiddenItems) {
            return super.canNavigateToSource()
        }
        return true
    }

    override fun navigate(requestFocus: Boolean) {
        if (UnityExplorer.getInstance(project!!).myShowHiddenItems) {
            return super.navigate(requestFocus)
        }

        val packageJson = virtualFile.findChild("package.json")
        if (packageJson != null) {
            OpenFileDescriptor(project!!, packageJson).navigate(requestFocus)
        }
    }

    override fun getName() = packageData.details.displayName

    override fun update(presentation: PresentationData) {
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.BuiltInPackage)

        var newTooltip = ""
        if (name != virtualFile.name) {
            newTooltip = virtualFile.name + "\n"
        }
        if (!packageData.details.version.isEmpty()) {
            newTooltip += packageData.details.version
        }
        if (!packageData.details.description.isEmpty()) {
            newTooltip += "\n${packageData.details.description}"
        }
        presentation.tooltip = newTooltip
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        if (other is BuiltinPackageNode) {
            return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
        }
        else if (other is PackageNode) {
            return 1
        }
        // other is UnknownPackageNode
        return -1
    }
}

// Note that this might get a PackageData with source == PackageSource.Unknown
class UnknownPackageNode(project: Project, private val packageData: PackageData)
    : AbstractTreeNode<Any>(project, packageData), Comparable<AbstractTreeNode<*>> {

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
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        if (other is UnknownPackageNode) {
            return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
        }
        // other is PackageNode or BuiltinPackageNode
        return 1
    }
}

// TODO: What are "testables"?
class ManifestJson(val dependencies: Map<String, String>, val testables: Array<String>?, val registry: String?)

