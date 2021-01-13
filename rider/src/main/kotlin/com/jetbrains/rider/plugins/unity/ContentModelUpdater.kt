package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.ProjectManagerListener
import com.intellij.openapi.vfs.AsyncFileListener
import com.intellij.openapi.vfs.AsyncFileListener.ChangeApplier
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.events.VFileCreateEvent
import com.intellij.openapi.vfs.newvfs.events.VFileDeleteEvent
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import com.intellij.util.SingleAlarm
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageManagerListener
import com.jetbrains.rider.plugins.unity.packageManager.PackageSource
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.indexing.contentModel.ContentModelUserStore
import com.jetbrains.rider.projectView.indexing.contentModel.tryExclude
import com.jetbrains.rider.projectView.indexing.contentModel.tryInclude
import kotlinx.coroutines.Runnable
import java.io.File

// todo: This is a misuse of project listeners. ContentModelUpdater has API and stores state and even has Alarm-based events, what fits naturally here is a service with a bunch of listeners.
class ContentModelUpdater : ProjectManagerListener {
    private val excludedFolders = hashSetOf<File>()
    private val includedFolders = hashSetOf<File>()

    private fun onRootFoldersChanged(project: Project) {
        application.assertIsDispatchThread()

        val newExcludedFolders = mutableSetOf<File>()
        val newIncludedFolders = mutableSetOf<File>()

        // It's common practice to have extra folders in the root project folder, e.g. a backup copy of Library for
        // a different version of Unity, or target, which can be renamed instead of having to do a time consuming
        // reimport. Also, folders containing builds for targets such as iOS.
        // Exclude all folders apart from Assets, ProjectSettings and Packages. Make sure to explicitly include these
        // three folders. Without the explicit include, Assets + ProjectSettings are indexed, but Packages aren't.
        for (child in project.projectDir.children) {
            if (child != null && isRootFolder(project, child)) {
                val file = VfsUtil.virtualToIoFile(child)

                if (shouldInclude(child.name)) {
                    newIncludedFolders.add(file)
                }
                else if (!isInProject(project, child)){
                    newExcludedFolders.add(file)
                }
            }
        }

        for (p in PackageManager.getInstance(project).allPackages) {
            if (p.packageFolder != null && p.source != PackageSource.BuiltIn
                    && p.source != PackageSource.Embedded
                    && p.source != PackageSource.Unknown) {

                newIncludedFolders.add(VfsUtil.virtualToIoFile(p.packageFolder))
            }
        }

        val excludesChanged = excludedFolders != newExcludedFolders
        val includesChanged = includedFolders != newIncludedFolders
        if (excludesChanged || includesChanged) {
            // They shouldn't be attached as folders, but just make sure
            val contentModel = ContentModelUserStore.getInstance(project)
            contentModel.attachedFolders.removeAll(excludedFolders)
            contentModel.attachedFolders.removeAll(includedFolders)

            if (excludesChanged) {
                contentModel.explicitExcludes.removeAll(excludedFolders)
                contentModel.tryExclude(newExcludedFolders.toTypedArray(), false)

                excludedFolders.clear()
                excludedFolders.addAll(newExcludedFolders)
            }
            if (includesChanged) {
                contentModel.explicitIncludes.removeAll(includedFolders)
                contentModel.tryInclude(newIncludedFolders.toTypedArray(), false)

                includedFolders.clear()
                includedFolders.addAll(newIncludedFolders)
            }

            application.invokeLater {
                contentModel.update()
            }
        }
    }

    private fun isRootFolder(project: Project, file: VirtualFile): Boolean {
        return file.isDirectory && file.parent == project.projectDir
    }

    private fun isInProject(project:Project, file: VirtualFile): Boolean {
        return ProjectModelViewHost.getInstance(project).getItemsByVirtualFile(file).isNotEmpty()
    }

    private fun shouldInclude(name: String): Boolean {
        return name.equals("Assets", true)
            || name.equals("Packages", true)
            || name.equals("ProjectSettings", true)
    }

    private inner class Listener(private val project: Project, private val alarm: SingleAlarm) : AsyncFileListener, PackageManagerListener {
        override fun onPackagesUpdated() {
            alarm.cancelAndRequest()
        }

        override fun prepareChange(events: MutableList<out VFileEvent>): ChangeApplier? {
            var requiresUpdate = false

            events.forEach {
                ProgressManager.checkCanceled()
                when (it) {
                    is VFileCreateEvent -> {
                        requiresUpdate = requiresUpdate || (it.isDirectory && it.parent == project.projectDir)
                    }
                    is VFileDeleteEvent -> {
                        requiresUpdate = requiresUpdate || isRootFolder(project, it.file)
                    }
                }
            }

            if (!requiresUpdate) {
                return null
            }

            return object: ChangeApplier {
                override fun afterVfsChange() = alarm.cancelAndRequest()
            }
        }
    }

    override fun projectOpened(project: Project) {
        if (UnityProjectDiscoverer.getInstance(project).isUnityProject) {

            val alarm = SingleAlarm(Runnable { onRootFoldersChanged(project) }, 1000, ModalityState.any(), project)
            val listener = Listener(project, alarm)

            // Listen to add/remove of folders in the root of the project
            VirtualFileManager.getInstance().addAsyncFileListener(listener, project)

            // Listen for external packages that we should be indexing
            PackageManager.getInstance(project).addListener(listener)

            alarm.request()
        }
    }
}
