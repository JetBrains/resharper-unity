package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.components.ProjectComponent
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.*
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageManagerListener
import com.jetbrains.rider.plugins.unity.packageManager.PackageSource
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.indexing.contentModel.ContentModelUserStore
import com.jetbrains.rider.projectView.indexing.contentModel.tryExclude
import com.jetbrains.rider.projectView.indexing.contentModel.tryInclude
import com.jetbrains.rider.util.idea.application
import java.io.File

class ContentModelUpdater(private val project: Project,
                          private val unityProjectDiscoverer: UnityProjectDiscoverer,
                          private val contentModel: ContentModelUserStore)
    : ProjectComponent {

    private val projectModelViewHost: ProjectModelViewHost by lazy { ProjectModelViewHost.getInstance(project) }
    private val packageManager: PackageManager by lazy { PackageManager.getInstance(project) }
    private val allFolders = hashSetOf<File>()

    override fun projectOpened() {
        if (unityProjectDiscoverer.isUnityProject) {

            onRootFoldersChanged()

            val listener = Listener()
            VirtualFileManager.getInstance().addVirtualFileListener(listener, project)
            packageManager.addListener(listener)
        }
    }

    private fun onRootFoldersChanged() {

        contentModel.attachedFolders.removeAll(allFolders)
        contentModel.explicitExcludes.removeAll(allFolders)
        contentModel.explicitIncludes.removeAll(allFolders)

        val excludes = mutableListOf<File>()
        val includes = mutableListOf<File>()

        // It's common practice to have extra folders in the root project folder, e.g. a backup copy of Library for
        // a different version of Unity, or target, which can be renamed instead of having to do a time consuming
        // reimport. Also, folders containing builds for targets such as iOS.
        // Exclude all folders apart from Assets, ProjectSettings and Packages
        for (child in project.projectDir.children) {
            if (child != null && isRootFolder(child) && !isInProject(child)) {
                val file = VfsUtil.virtualToIoFile(child)

                if (shouldInclude(child.name)) {
                    includes.add(file)
                }
                else {
                    excludes.add(file)
                }
            }
        }

        for (p in packageManager.allPackages) {
            if (p.packageFolder != null && p.source != PackageSource.BuiltIn
                    && p.source != PackageSource.Embedded
                    && p.source != PackageSource.Unknown) {

                includes.add(VfsUtil.virtualToIoFile(p.packageFolder))
            }
        }

        contentModel.tryExclude(excludes.toTypedArray(), false)
        contentModel.tryInclude(includes.toTypedArray(), false)
        contentModel.update()

        allFolders.clear()
        allFolders.addAll(excludes)
        allFolders.addAll(includes)
    }

    private fun isRootFolder(file: VirtualFile): Boolean {
        return file.isDirectory && file.parent == project.projectDir
    }

    private fun isInProject(file: VirtualFile): Boolean {
        return projectModelViewHost.getItemsByVirtualFile(file).isNotEmpty()
    }

    private fun shouldInclude(name: String): Boolean {
        return name.equals("Assets", true)
                || name.equals("ProjectSettings", true)
                || name.equals("Packages", true)
    }

    private inner class Listener : VirtualFileListener, PackageManagerListener {

        override fun onRefresh(all: Boolean) {
            application.invokeLater { onRootFoldersChanged() }
        }

        override fun fileCreated(event: VirtualFileEvent) {
            if (isRootFolder(event.file)) {
                application.invokeLater { onRootFoldersChanged() }
            }
        }

        override fun fileDeleted(event: VirtualFileEvent) {
            if (isRootFolder(event.file)) {
                application.invokeLater { onRootFoldersChanged() }
            }
        }
    }
}