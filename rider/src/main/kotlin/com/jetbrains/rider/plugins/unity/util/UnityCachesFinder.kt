package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.SystemProperties
import java.net.URL
import java.nio.file.Path
import java.nio.file.Paths

class UnityCachesFinder {

    // The cache location is static, across all projects
    companion object {
        private val packagesCacheRoot: Path? by lazy(this::findPackagesCacheRoot)

        // registry is a user defined string, can be anything. We expect it to be a URL, e.g. https://packages.unity.com
        fun getPackagesCacheFolder(registry: String): VirtualFile? {
            val cache = packagesCacheRoot ?: return null

            val defaultRegistry = "packages.unity.com"
            val registryHost = try {
                URL(registry).host
            }
            catch(_: Throwable) {
                // TODO: Log
                if (registry.isNotEmpty()) { registry } else { defaultRegistry }
            }

            return findPackagesCache(cache, registryHost) ?: findPackagesCache(cache, defaultRegistry)
        }

        private fun findPackagesCacheRoot() = when {
            SystemInfo.isWindows -> Paths.get(System.getenv("LOCALAPPDATA")).resolve("Unity/cache/packages")
            SystemInfo.isMac -> Paths.get(SystemProperties.getUserHome()).resolve("Library/Unity/cache/packages")
            SystemInfo.isLinux -> {
                // TODO: What's the Linux cache path?
                null
            }
            else -> null
        }

        private fun findPackagesCache(cacheRoot: Path, registry: String): VirtualFile? {
            // Can throw if the user has entered a weird registry value
            return try {
                VfsUtil.findFile(cacheRoot.resolve(registry), true)
            }
            catch(_: Throwable) {
                null
            }
        }
    }
}