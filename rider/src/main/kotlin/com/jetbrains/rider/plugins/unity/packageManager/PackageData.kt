package com.jetbrains.rider.plugins.unity.packageManager

import com.intellij.openapi.vfs.VirtualFile

enum class PackageSource {
    Unknown,
    BuiltIn,
    Registry,
    Embedded,
    Local,
    LocalTarball,
    Git;

    fun isEditable(): Boolean {
        return this == Embedded || this == Local
    }
}

data class PackageData(val name: String, val packageFolder: VirtualFile?, val details: PackageDetails,
                       val source: PackageSource, val gitDetails: GitDetails? = null, val tarballLocation: String? = null) {
    companion object {
        fun unknown(name: String, version: String, source: PackageSource = PackageSource.Unknown): PackageData {
            return PackageData(name, null, PackageDetails(name, "$name@$version", version,
                    "Cannot resolve package '$name' with version '$version'", "", mapOf()), source)
        }
    }
}

// Canonical name is the name from package.json, or the package's folder name if missing
// Display name is the display name from package.json, falling back to package.json name and then folder name
// For unresolved packages, name is the name from manifest.json and display name is name@version from manifest.json
data class PackageDetails(val canonicalName: String, val displayName: String, val version: String,
                          val description: String, val author: String, val dependencies: Map<String, String>) {
    companion object {
        fun fromPackageJson(packageFolder: VirtualFile, packageJson: PackageJson?): PackageDetails? {
            if (packageJson == null) return null
            val name = packageJson.name ?: packageFolder.name
            return PackageDetails(name, packageJson.displayName
                    ?: name, packageJson.version ?: "",
                    packageJson.description ?: "", getAuthor(packageJson.author), packageJson.dependencies ?: mapOf())
        }

        private fun getAuthor(author: Any?): String {
            if (author == null)
                return ""

            if (author is String)
                return author


            if (author is Map<*, *>) {
               return author["name"] as String? ?: "";
            }

            return "";
        }
    }
}

data class GitDetails(val url: String, val hash: String, val revision: String?)

// Git lock details have moved to packages-lock.json in Unity 2019.4+
class LockDetails(val hash: String?, val revision: String?)
class ManifestJson(val dependencies: Map<String, String>, val testables: Array<String>?, val registry: String?, val lock: Map<String, LockDetails>?)


// Other properties are available: category, keywords, unity (supported version)
data class PackageJson(val name: String?, val displayName: String?, val version: String?, val description: String?,
                       val author: Any?, val dependencies: Map<String, String>?)


// packages-lock.json (note the 's', this isn't NPM's package-lock.json)
// This was introduced in Unity 2019.4 and appears to be a full list of packages, dependencies and transitive
// dependencies. It also contains the git hash for git based packages.
// By observation:
// * `source` can be `builtin`, `registry`, `embedded`, `git`. Likely also includes other members of PackageSource, such
//    as local and local tarball
// * `version` is a semver value for `builtin`, `registry`, a `file:` url for `embedded` and a url for `git`
// * `url` is only available for registry packages, and is the url of the registry, e.g. https://packages.unity.com
// * `hash` is the commit hash for git packages
// * `dependencies` is a map of package name to version
// * `depth` is unknown, but could be an indicator of a transitive dependency rather than a direct dependency. E.g.
//    a package only used as a dependency of another package can have a depth of 1, while the parent package has a depth
//    of 0
class PackagesLockDependency(val version: String, val depth: Int?, val source: String?, val dependencies: Map<String, String>, val url: String?, val hash: String?)
class PackagesLockJson(val dependencies: Map<String, PackagesLockDependency>)
