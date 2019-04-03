package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.components.ProjectComponent
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.*
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.indexing.contentModel.ContentModelUserStore
import com.jetbrains.rider.projectView.indexing.contentModel.tryExclude
import com.jetbrains.rider.projectView.indexing.contentModel.tryInclude
import java.io.File

class ContentModelUpdater(private val project: Project,
                          private val unityProjectDiscoverer: UnityProjectDiscoverer,
                          private val contentModel: ContentModelUserStore)
    : ProjectComponent {

    override fun projectOpened() {
        if (unityProjectDiscoverer.isUnityProject) {

            // It's common practice to have extra folders in the root project folder, e.g. a backup copy of Library for
            // a different version of Unity, or target, which can be renamed instead of having to do a time consuming
            // reimport. Also, folders containing builds for targets such as iOS.
            // Exclude all folders apart from Assets, ProjectSettings and Packages
            val baseDir = project.projectDir
            val excludes = mutableListOf<File>()
            val includes = mutableListOf<File>()
            for (child in baseDir.children) {
                if (isRootFolder(child)) {
                    val file = VfsUtil.virtualToIoFile(child)
                    if (child.name.equals("Assets", true)
                            || child.name.equals("ProjectSettings", true)
                            || child.name.equals("Packages", true)) {
                        includes.add(file)
                    }
                    else {
                        excludes.add(file)
                    }
                }
            }

            // TODO: Include all referenced package folders from various cache folders

            contentModel.tryInclude(includes.toTypedArray(), false)
            contentModel.tryExclude(excludes.toTypedArray(), true)

            val listener = FileListener(contentModel)
            VirtualFileManager.getInstance().addVirtualFileListener(listener, project)
        }
    }

    private fun isRootFolder(file: VirtualFile?): Boolean {
        return file != null && file.isDirectory && file.parent == project.projectDir
    }

    private inner class FileListener(private val contentModel: ContentModelUserStore)
        : VirtualFileListener {

        override fun fileCreated(event: VirtualFileEvent) {
            // TODO: How to keep folders that are part of a project?
            if (isRootFolder(event.file)) {
                val file = VfsUtil.virtualToIoFile(event.file)
                contentModel.tryExclude(arrayOf(file), true)
            }
        }

        override fun fileDeleted(event: VirtualFileEvent) {
            // TODO: Is this right?
            if (isRootFolder(event.file)) {
                val file = VfsUtil.virtualToIoFile(event.file)
                contentModel.explicitExcludes.remove(file)
                contentModel.update()
            }
        }
    }
}