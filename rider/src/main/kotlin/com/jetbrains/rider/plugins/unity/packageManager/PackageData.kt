package com.jetbrains.rider.plugins.unity.packageManager

import com.intellij.openapi.vfs.VirtualFile

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

data class PackageData(val name: String, val packageFolder: VirtualFile?, val details: PackageDetails,
                       val source: PackageSource, val gitDetails: GitDetails? = null) {
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

data class GitDetails(val url: String, val revision: String, val hash: String)

// Other properties are available: category, keywords, unity (supported version)
data class PackageJson(val name: String?, val displayName: String?, val version: String?, val description: String?,
                       val author: Any?, val dependencies: Map<String, String>?)

