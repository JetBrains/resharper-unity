package com.jetbrains.rider.plugins.unity.packageManager

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.EventDispatcher
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.platform.util.idea.getOrCreateUserData
import com.jetbrains.rider.model.unity.frontendBackend.UnityPackage
import java.util.*

interface PackageManagerListener : EventListener {
    // This method is called on the UI thread
    fun onPackagesUpdated()
}

// There is chance of a race condition here. We don't know if the backend has started or completed the first read. We
// also don't know if we're in the middle of an update. If someone calls `hasPackage`, how confident can they be that
// a) we've got the initial list of packages and b) we've not just deleted the package because we're about to add a new
// version?
// (See OpenUnityProjectAFolderNotification)
// Should we include an "updating" flag, and only send the notification once it's been reset? That doesn't help with
// hasPackage - what if someone calls hasPackage and we're in the middle of updating?
class PackageManager(private val project: Project) {

    companion object {
        private val KEY: Key<PackageManager> = Key("UnityExplorer::PackageManager")
        private val logger = Logger.getInstance(PackageManager::class.java)

        fun getInstance(project: Project) = project.getOrCreateUserData(KEY) { PackageManager(project) }
    }

    private val packages = mutableMapOf<String, PackageData>()
    private val packagesByFolder = mutableMapOf<VirtualFile, PackageData>()

    // We don't support a granular add/remove notification, just a batched "changed" message
    // Our only subscriber for events right now is the Unity Explorer, and that works best by refreshing a parent node
    // and getting all packages
    private val listeners = EventDispatcher.create(PackageManagerListener::class.java)

    private var updating = false

    // TODO: Threading issues adding/removing and accessing?

    fun getPackages(): List<PackageData> = packages.values.toList()

    fun hasPackage(id: String) = packages.containsKey(id)
    fun tryGetPackage(id: String): PackageData? = packages[id]
    fun tryGetPackage(packageFolder: VirtualFile): PackageData? = packagesByFolder[packageFolder]

    fun addPackage(id: String, pack: UnityPackage) {
        if (!updating) {
            logger.error("Adding Unity package $id without startUpdate")
        }
        logger.trace("Adding Unity package: $id")
        val packageData = PackageData.fromUnityPackage(pack)
        packages[id] = packageData
        packageData.packageFolder?.let { packagesByFolder[it] = packageData }
    }

    fun removePackage(id: String) {
        if (!updating) {
            logger.error("Removing Unity package $id without startUpdate")
        }
        logger.trace("Removing Unity package: $id")
        packages.remove(id)?.let { packagesByFolder.remove(it.packageFolder) }
    }

    fun startUpdate() {
        updating = true
    }

    fun endUpdate() {
        if (!updating) {
            logger.warn("endUpdate called without startUpdate")
        }
        updating = false
        notifyPackagesChanged()
    }

    // Listeners are called back on a pooled thread!
    fun addListener(listener: PackageManagerListener) {
        // Automatically scoped to project lifetime
        listeners.addListener(listener, project)
    }

    private fun notifyPackagesChanged() {
        // Make sure to call back on the main thread
        application.invokeLater {
            listeners.multicaster.onPackagesUpdated()
        }
    }
}
