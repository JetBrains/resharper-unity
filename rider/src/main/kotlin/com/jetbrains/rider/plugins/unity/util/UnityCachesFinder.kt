package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.diagnostic.Logger
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
        private val logger = Logger.getInstance(UnityCachesFinder::class.java)
        private val packagesCacheRoot: Path? by lazy(this::findPackagesCacheRoot)

        // registry is a user defined string, can be anything. We expect it to be a URL, e.g. https://packages.unity.com
        fun getPackagesCacheFolder(registry: String): VirtualFile? {
            val cache = packagesCacheRoot ?: return null

            val defaultRegistry = "packages.unity.com"
            val registryHost = try {
                URL(registry).host
            }
            catch(throwable: Throwable) {
                val reg = if (registry.isNotEmpty()) { registry } else { defaultRegistry }
                logger.error("Error parsing registry as URL. Falling back to $reg", throwable)
                reg
            }

            return findPackagesCache(cache, registryHost) ?: findPackagesCache(cache, defaultRegistry)
        }

        private fun findPackagesCacheRoot() = when {
            SystemInfo.isWindows -> Paths.get(System.getenv("LOCALAPPDATA")).resolve("Unity/cache/packages")
            SystemInfo.isMac -> Paths.get(SystemProperties.getUserHome()).resolve("Library/Unity/cache/packages")
            SystemInfo.isLinux -> {
                val configRoot = System.getenv("XDG_CONFIG_HOME")?.let { Paths.get(it) }
                        ?: Paths.get(SystemProperties.getUserHome()).resolve(".config")
                configRoot.resolve("unity3d/cache/packages")
            }
            else -> null
        }

        private fun findPackagesCache(cacheRoot: Path, registry: String): VirtualFile? {
            // Path#resolve can throw if the user entered a weird registry value that isn't a proper path
            return try {
                VfsUtil.findFile(cacheRoot.resolve(registry), true)
            }
            catch(throwable: Throwable) {
                logger.error("Error looking for registry cache location: $cacheRoot/$registry", throwable)
                null
            }
        }
    }
}