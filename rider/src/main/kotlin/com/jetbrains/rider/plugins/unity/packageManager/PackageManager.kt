package com.jetbrains.rider.plugins.unity.packageManager

import com.google.gson.Gson
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.ReadAction
import com.intellij.openapi.diagnostic.ControlFlowException
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.AsyncFileListener
import com.intellij.openapi.vfs.AsyncFileListener.ChangeApplier
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import com.intellij.util.EventDispatcher
import com.intellij.util.concurrency.NonUrgentExecutor
import com.intellij.util.io.inputStream
import com.intellij.util.io.isDirectory
import com.intellij.util.pooledThreadSingleAlarm
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.util.idea.getOrCreateUserData
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.SemVer
import com.jetbrains.rider.plugins.unity.util.UnityCachesFinder
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.findFile
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solution
import java.lang.Integer.min
import java.nio.file.Path
import java.nio.file.Paths
import java.util.*

interface PackageManagerListener : EventListener {
    // This method is called on the UI thread
    fun onPackagesUpdated()
}

class PackageManager(private val project: Project) {

    companion object {
        private val KEY: Key<PackageManager> = Key("UnityExplorer::PackageManager")
        private val logger = Logger.getInstance(PackageManager::class.java)

        fun getInstance(project: Project) = project.getOrCreateUserData(KEY) { PackageManager(project) }

        private const val MILLISECONDS_BEFORE_REFRESH = 1000
        private const val DEFAULT_REGISTRY_URL = "https://packages.unity.com"
    }

    private val gson = Gson()
    private val listeners = EventDispatcher.create(PackageManagerListener::class.java)
    private val alarm = pooledThreadSingleAlarm(MILLISECONDS_BEFORE_REFRESH, project, ::refreshAndNotify)

    // The manifest of packages that ship with the editor. Used during package resolve to ensure that builtin packages
    // have a known minimum version
    private var lastReadGlobalManifestPath: String? = null
    private var globalManifest: EditorManifestJson? = null

    private var packagesByCanonicalName: Map<String, PackageData> = mutableMapOf()
    private var packagesByFolderPath: Map<String, PackageData> = mutableMapOf()

    private data class Packages(val packagesByCanonicalName: Map<String, PackageData>, val packagesByFolderPath: Map<String, PackageData>)

    init {
        val listener = PackagesAsyncFileListener()
        VirtualFileManager.getInstance().addAsyncFileListener(listener, project)

        val lifetime = project.lifetime

        // The application path affects the module packages. This comes from the backend, so will be up to date with
        // changes from the Editor via protocol, or changes to the project files via heuristics
        project.solution.rdUnityModel.unityApplicationData.advise(lifetime) { scheduleRefreshAndNotify() }

        scheduleRefreshAndNotify()
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

    fun hasPackage(id:String):Boolean {
        return allPackages.any { it.name == id }
    }

    fun getPackageData(packageFolder: VirtualFile): PackageData? {
        return packagesByFolderPath[packageFolder.path]
    }

    fun getPackageData(canonicalName: String): PackageData? {
        return packagesByCanonicalName[canonicalName]
    }

    fun addListener(listener: PackageManagerListener) {
        // Automatically scoped to project lifetime
        listeners.addListener(listener, project)
    }

    private fun scheduleRefreshAndNotify() {
        alarm.cancelAndRequest()
    }

    private fun refreshAndNotify() {
        // Get package data on a background thread
        ReadAction.nonBlocking<Packages> { getPackages() }
            .expireWith(project)
            .finishOnUiThread(ModalityState.any()) {
                // Updated without locks. Each reference is updated atomically, and they are not used together, so there
                // are no tearing issues
                packagesByCanonicalName = it.packagesByCanonicalName
                packagesByFolderPath = it.packagesByFolderPath
                listeners.multicaster.onPackagesUpdated()
            }
            .submit(NonUrgentExecutor.getInstance())
    }

    private data class EditorPackageDetails(val introduced: String?, val minimumVersion: String?, val version: String?)
    private data class EditorManifestJson(val recommended: Map<String, String>?, val defaultDependencies: Map<String, String>?, val packages: Map<String, EditorPackageDetails>?)

    private fun getPackages(): Packages {
        logger.debug("Refreshing packages manager")
        return getPackagesFromPackagesLockJson() ?: getPackagesFromManifestJson()
    }

    // Introduced officially in 2019.4, but available behind a switch in manifest.json in 2019.3
    // https://forum.unity.com/threads/add-an-option-to-auto-update-packages.730628/#post-4931882
    private fun getPackagesFromPackagesLockJson(): Packages? {
        val packagesLockJsonFile = getPackagesLockJsonFile() ?: return null

        logger.debug("Getting packages from packages-lock.json")

        val builtInPackagesFolder = UnityInstallationFinder.getInstance(project).getBuiltInPackagesRoot()

        val byCanonicalName: MutableMap<String, PackageData> = mutableMapOf()
        val byFolderPath: MutableMap<String, PackageData> = mutableMapOf()

        // This file contains all packages, including transitive dependencies and folders
        val packagesLock = readPackagesLockFile(packagesLockJsonFile) ?: return null

        for ((name, details) in packagesLock.dependencies) {

            // Note that packages-lock.json doesn't seem to use a version of "exclude" to indicate that a package has
            // been disabled, unlike older manifest.json versions. It just removes it from the manifest + lock files

            val packageData = getPackageData(packagesFolder, name, details, builtInPackagesFolder)
            byCanonicalName[name] = packageData
            if (packageData.packageFolder != null) {
                byFolderPath[packageData.packageFolder.path] = packageData
            }
        }

        return Packages(byCanonicalName, byFolderPath)
    }

    private fun getPackagesFromManifestJson(): Packages {

        logger.debug("Getting packages from manifest.json")

        val byCanonicalName: MutableMap<String, PackageData> = mutableMapOf()
        val byFolderPath: MutableMap<String, PackageData> = mutableMapOf()

        val builtInPackagesFolder = UnityInstallationFinder.getInstance(project).getBuiltInPackagesRoot()
        val manifestJsonFile = getManifestJsonFile() ?: return Packages(byCanonicalName, byFolderPath)

        val globalManifestPath = UnityInstallationFinder.getInstance(project).getPackageManagerDefaultManifest()
        if (globalManifestPath != null && globalManifestPath.toString() != lastReadGlobalManifestPath) {
            globalManifest = readGlobalManifestFile(globalManifestPath)
        }

        val projectManifest = readProjectManifestFile(manifestJsonFile)

        val registry = projectManifest.registry ?: DEFAULT_REGISTRY_URL
        for ((name, version) in projectManifest.dependencies) {
            if (version.equals("exclude", true)) continue

            val lockDetails = projectManifest.lock?.get(name)
            val packageData = getPackageData(packagesFolder, name, version, registry, builtInPackagesFolder, lockDetails)
            byCanonicalName[name] = packageData
            if (packageData.packageFolder != null) {
                byFolderPath[packageData.packageFolder.path] = packageData
            }
        }

        // From observation, Unity treats package folders in the Packages folder as actual packages, even if they're not
        // registered in manifest.json. They must have a */package.json file, in the root of the package itself
        for (child in packagesFolder.children) {
            val packageData = getPackageDataFromFolder(child.name, child, PackageSource.Embedded)
            if (packageData != null) {
                byCanonicalName[packageData.details.canonicalName] = packageData
                byFolderPath[child.path] = packageData
            }
        }

        // Calculate the transitive dependencies. This is all based on observation
        var packagesToProcess: Collection<PackageData> = byCanonicalName.values
        while (packagesToProcess.isNotEmpty()) {

            // This can't get stuck in an infinite loop. We look up each package in resolvedPackages - if it's already
            // there, it doesn't get processed any further, and we update resolvedPackages (well packagesByCanonicalName)
            // after every loop
            packagesToProcess = getPackagesFromDependencies(packagesFolder, registry, builtInPackagesFolder, byCanonicalName, packagesToProcess)
            for (newPackage in packagesToProcess) {
                byCanonicalName[newPackage.details.canonicalName] = newPackage
                newPackage.packageFolder?.let { byFolderPath[it.path] = newPackage }
            }
        }

        return Packages(byCanonicalName, byFolderPath)
    }

    private fun readGlobalManifestFile(editorManifestPath: Path): EditorManifestJson {
        lastReadGlobalManifestPath = editorManifestPath.toString()
        return try {
            gson.fromJson(editorManifestPath.inputStream().reader(), EditorManifestJson::class.java)
        } catch (e: Throwable) {
            if (e is ControlFlowException) {
                // Leave this null so we'll try and load again next time
                lastReadGlobalManifestPath = null
            } else {
                logger.error("Error deserializing Resources/PackageManager/Editor/manifest.json")
            }
            EditorManifestJson(emptyMap(), emptyMap(), emptyMap())
        }
    }

    private fun readProjectManifestFile(manifestFile: VirtualFile): ManifestJson {
        return try {
            gson.fromJson(manifestFile.inputStream.reader(), ManifestJson::class.java)
        } catch (e: Throwable) {
            if (e !is ControlFlowException) {
                logger.error("Error deserializing Packages/manifest.json", e)
            }
            ManifestJson(emptyMap(), emptyArray(), null, emptyMap())
        }
    }

    private fun readPackagesLockFile(packagesLockFile: VirtualFile): PackagesLockJson? = try {
        gson.fromJson(packagesLockFile.inputStream.reader(), PackagesLockJson::class.java)
    } catch (e: Throwable) {
        if (e !is ControlFlowException) {
            logger.error("Error deserializeing Packages/packages-lock.json", e)
        }
        null
    }

    private fun getPackagesLockJsonFile(): VirtualFile? {
        // Only exists in Unity 2019.4+
        return project.findFile("Packages/packages-lock.json")
    }

    private fun getManifestJsonFile(): VirtualFile? {
        return project.findFile("Packages/manifest.json")
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

                val minimumVersion = SemVer.parse(globalManifest?.packages?.get(name)?.minimumVersion ?: "")
                dependencies[name] = if (minimumVersion != null && minimumVersion > thisVersion) {
                    minimumVersion
                }
                else {
                    thisVersion
                }
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
                ?: getGitPackage(name, version, lockDetails?.hash, lockDetails?.revision)
                ?: getLocalPackage(packagesFolder, name, version)
                ?: getBuiltInPackage(name, version, builtInPackagesFolder)
                ?: PackageData.unknown(name, version)
        }
        catch (throwable: Throwable) {
            if (throwable !is ControlFlowException) {
                logger.error("Error resolving package $name", throwable)
            }
            PackageData.unknown(name, version)
        }
    }

    private fun getPackageData(packagesFolder: VirtualFile,
                               name: String,
                               details: PackagesLockDependency,
                               builtInPackagesFolder: Path?)
        : PackageData {

        return try {
            when (details.source) {
                "embedded" -> getEmbeddedPackage(packagesFolder, name)
                "registry" -> getRegistryPackage(name, details.version, details.url ?: DEFAULT_REGISTRY_URL)
                "builtin" -> getBuiltInPackage(name, details.version, builtInPackagesFolder)
                "git" -> getGitPackage(name, details.version, details.hash)
                "local" -> getLocalPackage(packagesFolder, name, details.version)
                else -> PackageData.unknown(name, details.version)
            } ?: PackageData.unknown(name, details.version)
        } catch (throwable: Throwable) {
            if (throwable !is ControlFlowException) {
                logger.error("Error resolving package $name", throwable)
            }
            PackageData.unknown(name, details.version)
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
            val packageFolder = VfsUtil.findFile(filePath, false)
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

    private fun getGitPackage(name: String, version: String, hash: String?, revision: String? = null): PackageData? {
        if (hash == null) return null

        // If we have lockDetails, we know this is a git based package, so always return something
        return try {
            // 2019.3 changed the format of the cached folder to only use the first 10 characters of the hash
            val packageFolder = project.findFile("Library/PackageCache/$name@${hash}")
                ?: project.findFile("Library/PackageCache/$name@${hash.substring(0, min(hash.length, 10))}")
            getPackageDataFromFolder(name, packageFolder, PackageSource.Git, GitDetails(version, hash, revision))
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
        // Unity 2018.3 introduced an additional layer of caching for registry based packages, local to the project, so
        // that any edits to the files in the package only affect this project. This is primarily for the API updater,
        // which would otherwise modify files in the per-user cache
        // NOTE: We use findChild here because name/version might contain illegal chars, e.g. "https://" which will
        // throw in refreshAndFindFile on Windows
        val packageCacheFolder = project.findFile("Library/PackageCache")
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
            val packageFolder = VfsUtil.findFile(builtInPackagesFolder.resolve(name), false)
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
                if (t !is ControlFlowException) {
                    logger.error("Error reading package.json", t)
                }
            }
        }
        return null
    }

    private fun filterPackagesBySource(source: PackageSource): List<PackageData> {
        return packagesByCanonicalName.filterValues { it.source == source }.values.toList()
    }

    private inner class PackagesAsyncFileListener : AsyncFileListener {
        override fun prepareChange(events: MutableList<out VFileEvent>): ChangeApplier? {
            var refreshPackages = false

            events.forEach {
                ProgressManager.checkCanceled()

                // Update on any kind of change/creation/deletion of the main manifest.json, any package.json or the
                // deletion/creation of the Packages folder
                val path = it.path
                if (path.endsWith("/Packages/manifest.json", true)
                    || path.endsWith("/package.json", true)
                    || path.endsWith("/Packages/packages-lock.json", true)
                    || path.endsWith("/Packages", true)) {

                    refreshPackages = true
                }
            }

            if (!refreshPackages) return null

            return object: ChangeApplier {
                override fun afterVfsChange() = scheduleRefreshAndNotify()
            }
        }
    }
}