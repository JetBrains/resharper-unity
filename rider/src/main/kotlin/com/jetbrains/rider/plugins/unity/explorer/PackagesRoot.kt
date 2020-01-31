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
import com.jetbrains.rider.projectView.views.FileSystemNodeBase
import com.jetbrains.rider.projectView.views.SolutionViewNode
import com.jetbrains.rider.projectView.views.addNonIndexedMark
import com.jetbrains.rider.projectView.views.fileSystemExplorer.FileSystemExplorerNode
import com.jetbrains.rider.projectView.views.navigateToSolutionView
import icons.UnityIcons

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

class PackagesRoot(project: Project, private val packageManager: PackageManager)
    : UnityExplorerNode(project, packageManager.packagesFolder, listOf(), false) {

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
        packageManager.localPackages.forEach { addPackage(children, it) }
        packageManager.unknownPackages.forEach { addPackage(children, it) }

        if (packageManager.immutablePackages.any())
            children.add(0, ReadOnlyPackagesRoot(project!!, packageManager))

        return children
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        if (virtualFile.isDirectory) {
            packageManager.getPackageData(virtualFile)?.let {
                val embeddedPackageData = PackageData(it.name, virtualFile, it.details, PackageSource.Embedded)
                return PackageNode(project!!, packageManager, virtualFile, embeddedPackageData)
            }
        }
        return super.createNode(virtualFile, nestedFiles)
    }

    // Required for "Locate in Solution Explorer" to work. If we return false, the solution view visitor stops walking.
    // True is effectively "maybe"
    override fun contains(file: VirtualFile) = true

    private fun addPackage(children: MutableList<AbstractTreeNode<*>>, thePackage: PackageData) {
        if (thePackage.packageFolder != null) {
            children.add(PackageNode(project!!, packageManager, thePackage.packageFolder, thePackage))
        }
        else {
            children.add(UnknownPackageNode(project!!, thePackage))
        }
    }
}

class PackageNode(project: Project, private val packageManager: PackageManager, packageFolder: VirtualFile, private val packageData: PackageData)
    : UnityExplorerNode(project, packageFolder, listOf(), false, !packageData.source.isEditable()), Comparable<AbstractTreeNode<*>> {

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
        if (UnityExplorer.getInstance(myProject).showProjectNames)
            addProjects(presentation)

        val existingTooltip = presentation.tooltip ?: ""

        var tooltip = "<html>" + getPackageTooltip(name, packageData)
        tooltip += when (packageData.source) {
            PackageSource.Embedded -> if (virtualFile.name != name) "<br/><br/>Folder name: ${virtualFile.name}" else ""
            PackageSource.Local -> "<br/><br/>Folder location: ${virtualFile.path}"
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
            children.add(0, DependenciesRoot(project!!, packageManager, packageData))
        }

        return children
    }

    override fun compareTo(other: AbstractTreeNode<*>): Int {
        // Compare by name, rather than ID
        return String.CASE_INSENSITIVE_ORDER.compare(name, other.name)
    }
}

class DependenciesRoot(project: Project, private val packageManager: PackageManager, private val packageData: PackageData)
    : AbstractTreeNode<Any>(project, packageData) {

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Dependencies"
        presentation.setIcon(UnityIcons.Explorer.DependenciesRoot)
    }

    override fun getChildren(): MutableCollection<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()
        for ((name, version) in packageData.details.dependencies) {
            children.add(DependencyItemNode(project!!, packageManager, name, version))
        }
        return children
    }
}

class DependencyItemNode(project: Project, private val packageManager: PackageManager, private val packageName: String, version: String)
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
        project!!.navigateToSolutionView(packageData.packageFolder, requestFocus)
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

class ReadOnlyPackagesRoot(project: Project, private val packageManager: PackageManager)
    : CompositeFolderRoot(project, key) {

    companion object {
        val key = Any()
    }

    override fun update(presentation: PresentationData) {
        presentation.presentableText = "Read only"
        presentation.setIcon(UnityIcons.Explorer.ReadOnlyPackagesRoot)
    }

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {
        val children = mutableListOf<AbstractTreeNode<*>>()

        if (packageManager.hasBuiltInPackages)
            children.add(BuiltinPackagesRoot(project!!, packageManager))

        for (packageData in packageManager.immutablePackages) {
            if (packageData.source == PackageSource.BuiltIn) continue

            if (packageData.packageFolder == null) {
                children.add(UnknownPackageNode(project!!, packageData))
            }
            else {
                children.add(PackageNode(project!!, packageManager, packageData.packageFolder, packageData))
                addPackageFolder(packageData.packageFolder)
            }
        }
        return children
    }
}

class BuiltinPackagesRoot(project: Project, private val packageManager: PackageManager)
    : CompositeFolderRoot(project, key) {

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
                children.add(UnknownPackageNode(project!!, packageData))
            }
            else {
                children.add(BuiltinPackageNode(project!!, packageData))
                addPackageFolder(packageData.packageFolder)
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
    : FileSystemNodeBase(project, packageData.packageFolder!!, listOf()) {

    override fun calculateChildren(): MutableList<AbstractTreeNode<*>> {

        if (!UnityExplorer.getInstance(project!!).showHiddenItems) {
            return arrayListOf()
        }
        return super.calculateChildren()
    }

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        return FileSystemExplorerNode(project!!, virtualFile, nestedFiles, false)
    }

    override fun canNavigateToSource(): Boolean {
        if (UnityExplorer.getInstance(project!!).showHiddenItems) {
            return super.canNavigateToSource()
        }
        return true
    }

    override fun navigate(requestFocus: Boolean) {
        if (UnityExplorer.getInstance(project!!).showHiddenItems) {
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

        val tooltip = getPackageTooltip(name, packageData)
        if (tooltip != name) {
            presentation.tooltip = tooltip
        }
    }
}

// Note that this might get a PackageData with source == PackageSource.Unknown
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

class LockDetails(val hash: String?, val revision: String?)

// TODO: What are "testables"?
class ManifestJson(val dependencies: Map<String, String>, val testables: Array<String>?, val registry: String?, val lock: Map<String, LockDetails>?)

