package com.jetbrains.rider.plugins.unity.explorer

import com.google.gson.Gson
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.io.isDirectory
import com.jetbrains.rdclient.util.idea.getOrCreateUserData
import com.jetbrains.rider.plugins.unity.util.SemVer
import com.jetbrains.rider.plugins.unity.util.UnityCachesFinder
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.refreshAndFindFile
import com.jetbrains.rider.projectDir
import java.nio.file.Path
import java.nio.file.Paths

enum class PackageSource {
    Unknown,
    BuiltIn,
    Registry,
    Embedded,
    Local,
    Git;

    fun isEditable(): Boolean {
        return this == Embedded || this == Local
    }
}

data class PackageData(val name: String, val packageFolder: VirtualFile?, val details: PackageDetails, val source: PackageSource) {
    companion object {
        fun unknown(name: String, version: String, source: PackageSource = PackageSource.Unknown): PackageData {
            return PackageData(name, null, PackageDetails(name, "$name@$version", version, "", mapOf()), source)
        }
    }
}

data class PackageDetails(val canonicalName: String, val displayName: String, val version: String, val description: String, val dependencies: Map<String, String>) {
    companion object {
        fun fromPackageJson(packageFolder: VirtualFile, packageJson: PackageJson?): PackageDetails? {
            if (packageJson == null) return null
            val name = packageJson.name ?: packageFolder.name
            return PackageDetails(name, packageJson.displayName ?: name, packageJson.version ?: "", packageJson.description ?: "", packageJson.dependencies ?: mapOf())
        }
    }
}

// Other properties are available: category, keywords, unity (supported version), author
data class PackageJson(val name: String?, val displayName: String?, val version: String?, val description: String?, val dependencies: Map<String, String>?)

class PackagesManager(private val project: Project) {

    companion object {
        private val key: Key<PackagesManager> = Key("UnityExplorer::PackagesManager")
        private val logger = Logger.getInstance(PackagesManager::class.java)

        fun getInstance(project: Project) = project.getOrCreateUserData(key) { PackagesManager(project) }
    }

    private val gson = Gson()
    private val packagesByCanonicalName: MutableMap<String, PackageData> = mutableMapOf()
    private val packagesByFolderName: MutableMap<String, PackageData> = mutableMapOf()

    init {
        refresh()
    }

    val packagesFolder: VirtualFile
        get() = project.projectDir.findChild("Packages")!!

    val hasPackages: Boolean
        get() = packagesByCanonicalName.isNotEmpty()

    val localPackages: List<PackageData>
        get() = filterPackagesBySource(PackageSource.Local).toList()

    val immutablePackages: List<PackageData>
        get() = packagesByCanonicalName.filterValues { !it.source.isEditable() }.values.toList()

    val hasBuiltInPackages: Boolean
        get() = filterPackagesBySource(PackageSource.BuiltIn).any()

    fun getPackageData(packageFolder: VirtualFile): PackageData? {
        return packagesByFolderName[packageFolder.name]
    }

    fun getPackageData(canonicalName: String): PackageData? {
        return packagesByCanonicalName[canonicalName]
    }

    fun refresh() {
        logger.debug("Refreshing packages manager")

        packagesByCanonicalName.clear()
        packagesByFolderName.clear()

        val manifestJson = getManifestJsonFile() ?: return
        val builtInPackagesFolder = UnityInstallationFinder.getInstance(project).getBuiltInPackagesRoot()

        val manifest = try {
            gson.fromJson(manifestJson.inputStream.reader(), ManifestJson::class.java)
        } catch (e: Throwable) {
            logger.error("Error deserializing Packages/manifest.json", e)
            ManifestJson(emptyMap(), emptyArray(), null)
        }

        val registry = manifest.registry ?: "https://packages.unity.com"
        for ((name, version) in manifest.dependencies) {
            if (version.equals("exclude", true)) continue

            val packageData = getPackageData(packagesFolder, name, version, registry, builtInPackagesFolder)
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
            newPackages.add(getPackageData(packagesFolder, name, version.toString(), registry, builtInPackagesFolder))
        }
        return newPackages
    }

    private fun getPackageData(packagesFolder: VirtualFile, name: String, version: String, registry: String, builtInPackagesFolder: Path?): PackageData {
        return getLocalPackage(packagesFolder, name, version)
                ?: getGitPackage(name, version)
                ?: getEmbeddedPackage(packagesFolder, name)
                ?: getRegistryPackage(name, version, registry)
                ?: getBuiltInPackage(name, version, builtInPackagesFolder)
                ?: PackageData.unknown(name, version)
    }

    private fun getLocalPackage(packagesFolder: VirtualFile, name: String, version: String): PackageData? {

        // If it begins with file: it's a path to the package, either relative to the Packages folder or fully qualified
        if (version.startsWith("file:")) {
            return try {
                val path = version.substring(5)
                val filePath = Paths.get(packagesFolder.path, path)
                val packageFolder = VfsUtil.findFile(filePath, true)
                if (packageFolder != null && packageFolder.isDirectory) {
                    getPackageDataFromFolder(name, packageFolder, PackageSource.Local)
                } else {
                    // It should be a local package, but it's broken
                    PackageData.unknown(name, version)
                }
            } catch (throwable: Throwable) {
                logger.error("Error finding local package", throwable)
                null
            }
        }
        return null
    }

    @Suppress("UNUSED_PARAMETER")
    private fun getGitPackage(name: String, version: String): PackageData? {
        // Not yet documented/supported in Unity
        return null
    }

    private fun getEmbeddedPackage(packagesFolder: VirtualFile, name: String): PackageData? {
        val packageFolder = packagesFolder.findChild(name)
        return getPackageDataFromFolder(name, packageFolder, PackageSource.Embedded)
    }

    private fun getRegistryPackage(name: String, version: String, registry: String): PackageData? {
        val registryRoot = UnityCachesFinder.getPackagesCacheFolder(registry)
        if (registryRoot == null || !registryRoot.isDirectory) return null

        val packageFolder = registryRoot.findChild("$name@$version")
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

    private fun getPackageDataFromFolder(name: String, packageFolder: VirtualFile?, source: PackageSource): PackageData? {
        packageFolder?.let {
            if (packageFolder.isDirectory) {
                val packageDetails = readPackagesJson(packageFolder)
                if (packageDetails != null) {
                    return PackageData(name, packageFolder, packageDetails, source)
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
}