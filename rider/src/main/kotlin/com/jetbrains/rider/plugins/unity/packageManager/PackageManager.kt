package com.jetbrains.rider.plugins.unity.packageManager

import com.google.gson.Gson
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.*
import com.intellij.util.EventDispatcher
import com.intellij.util.io.isDirectory
import com.jetbrains.rdclient.util.idea.getOrCreateUserData
import com.jetbrains.rider.model.*
import com.jetbrains.rider.plugins.unity.explorer.LockDetails
import com.jetbrains.rider.plugins.unity.explorer.ManifestJson
import com.jetbrains.rider.plugins.unity.util.SemVer
import com.jetbrains.rider.plugins.unity.util.UnityCachesFinder
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.refreshAndFindFile
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.lifetime
import java.nio.file.Path
import java.nio.file.Paths
import java.util.*

interface PackageManagerListener : EventListener {
    fun onRefresh(all: Boolean)
}

class PackageManager(private val project: Project) {

    companion object {
        private val KEY: Key<PackageManager> = Key("UnityExplorer::PackageManager")
        private val logger = Logger.getInstance(PackageManager::class.java)

        fun getInstance(project: Project) = project.getOrCreateUserData(KEY) { PackageManager(project) }
    }

    private val gson = Gson()
    private val packagesByCanonicalName: MutableMap<String, PackageData> = mutableMapOf()
    private val packagesByFolderName: MutableMap<String, PackageData> = mutableMapOf()
    private val listeners = EventDispatcher.create(PackageManagerListener::class.java)

    init {
        refresh()

        val listener = FileListener(project)
        VirtualFileManager.getInstance().addVirtualFileListener(listener, project)

        val lifetime = project.lifetime

        // The application path affects the module packages
        project.solution.rdUnityModel.applicationPath.advise(lifetime) { refresh() }

        // Unity will rewrite solution/projects after resolving packages. If we don't listen for it, we might resolve to
        // an incorrect cache folder, or mark packages as unknown
        val projectModelViewHost = ProjectModelViewHost.getInstance(project)
        projectModelViewHost.addSignal.advise(lifetime) { onProjectModelChanged(it) }
        projectModelViewHost.updateSignal.advise(lifetime) { onProjectModelChanged(it) }
        projectModelViewHost.removeSignal.advise(lifetime) { onProjectModelChanged(it) }
    }

    val packagesFolder: VirtualFile
        get() = project.projectDir.findChild("Packages")!!

    val hasPackages: Boolean
        get() = packagesByCanonicalName.isNotEmpty()

    val allPackages: List<PackageData>
        get() = packagesByCanonicalName.values.toList()

    val localPackages: List<PackageData>
        get() = filterPackagesBySource(PackageSource.Local).toList()

    val immutablePackages: List<PackageData>
        get() = packagesByCanonicalName.filterValues { !it.source.isEditable() && it.source != PackageSource.Unknown }.values.toList()

    val unknownPackages: List<PackageData>
        get() = filterPackagesBySource(PackageSource.Unknown).toList()

    val hasBuiltInPackages: Boolean
        get() = filterPackagesBySource(PackageSource.BuiltIn).any()

    fun getPackageData(packageFolder: VirtualFile): PackageData? {
        return packagesByFolderName[packageFolder.name]
    }

    fun getPackageData(canonicalName: String): PackageData? {
        return packagesByCanonicalName[canonicalName]
    }

    fun addListener(listener: PackageManagerListener) {
        // Automatically scoped to project lifetime
        listeners.addListener(listener, project)
    }

    fun refresh(all: Boolean = false) {
        doRefresh()
        listeners.multicaster.onRefresh(all)
    }

    private fun doRefresh() {
        logger.debug("Refreshing packages manager")

        packagesByCanonicalName.clear()
        packagesByFolderName.clear()

        val manifestJson = getManifestJsonFile() ?: return
        val builtInPackagesFolder = UnityInstallationFinder.getInstance(project).getBuiltInPackagesRoot()

        val manifest = try {
            gson.fromJson(manifestJson.inputStream.reader(), ManifestJson::class.java)
        } catch (e: Throwable) {
            logger.error("Error deserializing Packages/manifest.json", e)
            ManifestJson(emptyMap(), emptyArray(), null, emptyMap())
        }

        val registry = manifest.registry ?: "https://packages.unity.com"
        for ((name, version) in manifest.dependencies) {
            if (version.equals("exclude", true)) continue

            val lockDetails = manifest.lock?.get(name)
            val packageData = getPackageData(packagesFolder, name, version, registry, builtInPackagesFolder, lockDetails)
            packagesByCanonicalName[name] = packageData
            if (packageData.packageFolder != null) {
                packagesByFolderName[packageData.packageFolder.name] = packageData
            }
        }

        // From observation, Unity treats package folders in the Packages folder as actual packages, even if they're not
        // registered in manifest.json
        for (child in packagesFolder.children) {
            val packageData = getPackageDataFromFolder(child.name, child, PackageSource.Embedded)
            if (packageData != null) {
                packagesByCanonicalName[packageData.details.canonicalName] = packageData
                packagesByFolderName[child.name] = packageData
            }
        }

        // Calculate the transitive dependencies. This is all based on observation
        val resolvedPackages = packagesByCanonicalName
        var packagesToProcess: Collection<PackageData> = packagesByCanonicalName.values
        while (packagesToProcess.isNotEmpty()) {

            // This can't get stuck in an infinite loop. We look up each package in resolvedPackages - if it's already
            // there, it doesn't get processed any further, and we update resolvedPackages (well packagesByCanonicalName)
            // after every loop
            packagesToProcess = getPackagesFromDependencies(packagesFolder, registry, builtInPackagesFolder, resolvedPackages, packagesToProcess)
            for (newPackage in packagesToProcess) {
                packagesByCanonicalName[newPackage.details.canonicalName] = newPackage
                newPackage.packageFolder?.let { packagesByFolderName[it.name] = newPackage }
            }
        }
    }

    private fun getManifestJsonFile(): VirtualFile? {
        return project.refreshAndFindFile("Packages/manifest.json")
    }

    private fun getPackagesFromDependencies(packagesFolder: VirtualFile, registry: String, builtInPackagesFolder: Path?,
                                            resolvedPackages: Map<String, PackageData>,
                                            packages: Collection<PackageData>)
            : Collection<PackageData> {

        val dependencies = mutableMapOf<String, SemVer>()

        // Find the highest requested version of each dependency of each package being processed
        for (packageData in packages) {

            for ((name, version) in packageData.details.dependencies) {

                // If it's been previously resolved, there's nothing more to do. Note that skipping it here means it's
                // not processed further, including dependencies
                if (resolvedPackages.containsKey(name)) continue

                val lastVersion = dependencies[name]
                val thisVersion = SemVer.parse(version)
                if (thisVersion == null || (lastVersion != null && lastVersion >= thisVersion)) continue

                dependencies[name] = thisVersion
            }
        }

        // Now find all of the packages for all of these dependencies
        val newPackages = mutableListOf<PackageData>()
        for ((name, version) in dependencies) {
            newPackages.add(getPackageData(packagesFolder, name, version.toString(), registry, builtInPackagesFolder, null))
        }
        return newPackages
    }

    private fun getPackageData(packagesFolder: VirtualFile,
                               name: String,
                               version: String,
                               registry: String,
                               builtInPackagesFolder: Path?,
                               lockDetails: LockDetails?)
            : PackageData {

        // Order is important here. Embedded packages in the Packages folder take precedence over everything. Registry
        // packages are the most likely, and can't clash with other packages, so put them high up. The file: protocol is
        // used by local and can also be a protocol for git (although I can't get it to work), so check git first
        return try {
            getEmbeddedPackage(packagesFolder, name)
                ?: getRegistryPackage(name, version, registry)
                ?: getGitPackage(name, version, lockDetails)
                ?: getLocalPackage(packagesFolder, name, version)
                ?: getBuiltInPackage(name, version, builtInPackagesFolder)
                ?: PackageData.unknown(name, version)
        }
        catch (throwable: Throwable) {
            logger.error("Error resolving package", throwable)
            PackageData.unknown(name, version)
        }
    }

    private fun getLocalPackage(packagesFolder: VirtualFile, name: String, version: String): PackageData? {

        if (!version.startsWith("file:")) {
            return null
        }

        // We know this is a "file:" based package, so always return something. It can be resolved relative to the
        // Packages folder, or as a fully qualified path
        return try {
            val path = version.substring(5)
            val packagesPath = Paths.get(packagesFolder.path)
            val filePath = packagesPath.resolve(path)
            val packageFolder = VfsUtil.findFile(filePath, true)
            if (packageFolder != null && packageFolder.isDirectory) {
                getPackageDataFromFolder(name, packageFolder, PackageSource.Local)
            } else {
                // It should be a local package, but it's broken
                PackageData.unknown(name, version)
            }
        } catch (throwable: Throwable) {
            logger.error("Error resolving local package", throwable)
            PackageData.unknown(name, version)
        }
    }

    private fun getGitPackage(name: String, version: String, lockDetails: LockDetails?): PackageData? {
        if (lockDetails == null) return null
        if (lockDetails.revision == null || lockDetails.hash == null) return null

        // If we have lockDetails, we know this is a git based package, so always return something
        return try {
            val packageFolder = project.refreshAndFindFile("Library/PackageCache/$name@${lockDetails.hash}")
            getPackageDataFromFolder(name, packageFolder, PackageSource.Git, GitDetails(version, lockDetails.revision, lockDetails.hash))
        }
        catch (throwable: Throwable) {
            logger.error("Error resolving git package", throwable)
            PackageData.unknown(name, version)
        }
    }

    private fun getEmbeddedPackage(packagesFolder: VirtualFile, name: String): PackageData? {
        val packageFolder = packagesFolder.findChild(name)
        return getPackageDataFromFolder(name, packageFolder, PackageSource.Embedded)
    }

    private fun getRegistryPackage(name: String, version: String, registry: String): PackageData? {
        // Unity 2018.3 introduced an additional layer of caching, local to the project, so that any edits to the files
        // in the package only affect this project. This is primarily for the API updater, which would otherwise modify
        // files in the per-user cache
        // NOTE: We use findChild here because name/version might contain illegal chars, e.g. "https://" which will
        // throw in refreshAndFindFile on Windows
        val packageCacheFolder = project.refreshAndFindFile("Library/PackageCache")
        var packageFolder = packageCacheFolder?.findChild("$name@$version")
        val packageData = getPackageDataFromFolder(name, packageFolder, PackageSource.Registry)
        if (packageData != null) return packageData

        val registryRoot = UnityCachesFinder.getPackagesCacheFolder(registry)
        if (registryRoot == null || !registryRoot.isDirectory) return null

        packageFolder = registryRoot.findChild("$name@$version")
        return getPackageDataFromFolder(name, packageFolder, PackageSource.Registry)
    }

    private fun getBuiltInPackage(name: String, version: String, builtInPackagesFolder: Path?): PackageData? {

        // If we can identify the module root of the current project, use it to look up the module
        if (builtInPackagesFolder?.isDirectory() == true) {
            val packageFolder = VfsUtil.findFile(builtInPackagesFolder.resolve(name), true)
            return getPackageDataFromFolder(name, packageFolder, PackageSource.BuiltIn)
        }

        // Simple heuristic to identify modules when we can't look them up in the correct location. Unity will control
        // the namespace of their registries, which makes it highly unlikely anyone else will create a package that
        // begins with `com.unity.modules.` And if they do, they only have themselves to blame.
        if (name.startsWith("com.unity.modules.")) {
            return PackageData.unknown(name, version, PackageSource.BuiltIn)
        }

        return null
    }

    private fun getPackageDataFromFolder(name: String, packageFolder: VirtualFile?, source: PackageSource,
                                         gitDetails: GitDetails? = null): PackageData? {
        packageFolder?.let {
            if (packageFolder.isDirectory) {
                val packageDetails = readPackagesJson(packageFolder)
                if (packageDetails != null) {
                    return PackageData(name, packageFolder, packageDetails, source, gitDetails)
                }
            }
        }
        return null
    }

    private fun readPackagesJson(packageFolder: VirtualFile): PackageDetails? {
        val packageFile = packageFolder.findChild("package.json")
        if (packageFile?.exists() == true && !packageFile.isDirectory) {
            try {
                val packageJson = gson.fromJson(packageFile.inputStream.reader(), PackageJson::class.java)
                return PackageDetails.fromPackageJson(packageFolder, packageJson!!)
            }
            catch (t: Throwable) {
                logger.error("Error reading package.json", t)
            }
        }
        return null
    }

    private fun filterPackagesBySource(source: PackageSource): List<PackageData> {
        return packagesByCanonicalName.filterValues { it.source == source }.values.toList()
    }

    private fun onProjectModelChanged(node: ProjectModelNode) {

        val descriptor = node.descriptor
        if (descriptor is RdSolutionDescriptor && descriptor.state == RdSolutionState.Ready) {
            refresh()
        }
        else if (descriptor is RdProjectDescriptor && descriptor.state == RdProjectState.Ready) {
            refresh()
        }
    }

    private inner class FileListener(private val project: Project) : VirtualFileListener {

        override fun contentsChanged(event: VirtualFileEvent) {
            // Note that we update if a package.json file changes because it might mean a change of dependencies
            if (isManifestJson(event.file) || isPackageJson(event.file)) {
                refresh()
            }
        }

        override fun fileCreated(event: VirtualFileEvent) {
            if (isPackagesFolder(event.file)) {
                refresh(true)
            }
            else if (isManifestJson(event.file)) {
                refresh()
            }
        }

        override fun fileDeleted(event: VirtualFileEvent) {
            if (isPackagesFolder(event.file)) {
                refresh(true)
            }
            else if (isManifestJson(event.file)) {
                refresh()
            }
        }

        private fun isPackagesFolder(file: VirtualFile?): Boolean {
            return file != null && file.name == "Packages" && file.parent == project.projectDir
        }

        private fun isManifestJson(file: VirtualFile?): Boolean {
            return file != null && file.name == "manifest.json" && isPackagesFolder(file.parent)
        }

        private fun isPackageJson(file: VirtualFile?): Boolean {
            // Ideally, this would be package.json that belonged to a package, but we don't have a way of knowing this.
            // We might be refreshing the packages list based on changes to a project file. This is acceptable because
            // Unity projects usually won't contain package.json
            return file != null && file.name == "package.json"
        }
    }
}