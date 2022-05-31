@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.Disposable
import com.intellij.openapi.command.CommandProcessor
import com.intellij.openapi.command.undo.UndoManager
import com.intellij.openapi.components.serviceIfCreated
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.ProjectManager
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.newvfs.BulkFileListener
import com.intellij.openapi.vfs.newvfs.events.*
import com.intellij.util.PathUtil
import com.intellij.util.application
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rd.platform.util.getLogger
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorerFileSystemNode.Companion.isHiddenAsset
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.workspace.getPackages
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.VfsBackendRequester
import org.jetbrains.annotations.Nls
import java.nio.file.Path
import java.nio.file.Paths
import java.time.LocalDateTime
import java.time.ZoneOffset
import java.util.*
import kotlin.io.path.name

class MetaTracker : BulkFileListener, VfsBackendRequester, Disposable {
    companion object {
        private val logger = getLogger<MetaTracker>()
        fun getMetaFileName(fileName: String) = "$fileName.meta"
        fun getMetaFile(path: String?): Path? {
            path ?: return null
            val file = Paths.get(path)
            val metaFileName = getMetaFileName(file.name)
            return file.parent.resolve(metaFileName)
        }
    }

    override fun after(events: MutableList<out VFileEvent>) {
        val projectManager = serviceIfCreated<ProjectManager>() ?: return
        val openedUnityProjects = projectManager.openProjects.filter { !it.isDisposed && it.isUnityProjectFolder() && !isUndoRedoInProgress(it)}.toList()
        val actions = MetaActionList()

        for (event in events) {
            if (!isValidEvent(event)) continue
            for (project in openedUnityProjects) {
                if (isApplicableForProject(event, project)) {
                    if (isMetaFile(event)) // Collect modified meta files at first (usually there is no such files, but still)
                        actions.addInitialSetOfChangedMetaFiles(Paths.get(event.path))
                    else {
                        try {
                            when (event) {
                                is VFileCreateEvent -> {
                                    val metaFileName = getMetaFileName(event.childName)
                                    val metaFile = event.parent.toNioPath().resolve(metaFileName)
                                    val ls = event.file?.detectedLineSeparator
                                        ?: "\n" // from what I see, Unity 2020.3 always uses "\n", but lets use same as the main file.
                                    actions.add(metaFile, project) {
                                        createMetaFile(event.file, event.parent, metaFileName, ls)
                                    }
                                }
                                is VFileDeleteEvent -> {
                                    val metaFile = Companion.getMetaFile(event.path) ?: continue
                                    actions.add(metaFile, project) {
                                        VfsUtil.findFile(metaFile, true)?.delete(this)
                                    }
                                }
                                is VFileCopyEvent -> {
                                    val metaFile = Companion.getMetaFile(event.file.path) ?: continue
                                    val ls = event.file.detectedLineSeparator ?: "\n"
                                    actions.add(metaFile, project) {
                                        createMetaFile(event.file, event.newParent,
                                            Companion.getMetaFileName(event.newChildName), ls)
                                    }
                                }
                                is VFileMoveEvent -> {
                                    val metaFile = Companion.getMetaFile(event.oldPath) ?: continue
                                    actions.add(metaFile, project) {
                                        VfsUtil.findFile(metaFile, true)?.move(this, event.newParent)
                                    }
                                }
                                is VFilePropertyChangeEvent -> {
                                    if (!event.isRename) continue
                                    val metaFile = Companion.getMetaFile(event.oldPath) ?: continue
                                    actions.add(metaFile, project) {
                                        val target = Companion.getMetaFileName(event.newValue as String)
                                        val origin = VfsUtil.findFile(metaFile, true)
                                        val conflictingMeta = origin?.parent?.findChild(target)
                                        if (conflictingMeta != null) {
                                            logger.warn("Removing conflicting meta $conflictingMeta")
                                            conflictingMeta.delete(this)
                                        }
                                        origin?.rename(this, target)
                                    }
                                }
                            }
                        } catch (t: Throwable) {
                            logger.error(t)
                            continue
                        }
                    }
                }
            }
        }

        if (actions.isEmpty()) return
        actions.execute()
    }

    private fun isValidEvent(event: VFileEvent):Boolean{
        if (event.isFromRefresh) return false
        if (event.fileSystem !is LocalFileSystem) return false
        return CommandProcessor.getInstance().currentCommand != null
    }

    private fun isUndoRedoInProgress(project: Project): Boolean {
        return UndoManager.getInstance(project).isUndoOrRedoInProgress
    }

    private fun isMetaFile(event: VFileEvent): Boolean {
        val extension = event.file?.extension ?: PathUtil.getFileExtension(event.path)
        return "meta".equals(extension, true)
    }

    private fun isApplicableForProject(event: VFileEvent, project: Project): Boolean {
        val file = event.file ?: return false
        val assets = project.projectDir.findChild("Assets") ?: return false
        if (VfsUtil.isAncestor(assets, file, false))
            return true

        val editablePackages = WorkspaceModel.getInstance(project).getPackages().filter { it.isEditable() }
        for (pack in editablePackages) {
            val packageFolder = pack.packageFolder ?: continue
            if (VfsUtil.isAncestor(packageFolder, file, false)) return true
        }

        return false
    }

    private fun createMetaFile(assetFile: VirtualFile?, parent: VirtualFile, metaFileName: String, ls: String) {
        if (assetFile != null && isHiddenAsset(assetFile)) return // not that children of a hidden folder (like `Documentation~`), would still pass this check. I think it is fine.
        val metaFile = parent.createChildData(this, metaFileName)
        val guid = UUID.randomUUID().toString().replace("-", "").substring(0, 32)
        val timestamp = LocalDateTime.now(ZoneOffset.UTC).atZone(ZoneOffset.UTC).toEpochSecond() // LocalDateTime to epoch seconds
        val content = "fileFormatVersion: 2${ls}guid: ${guid}${ls}timeCreated: $timestamp"
        VfsUtil.saveText(metaFile, content)
    }

    override fun dispose() = Unit

    private class MetaActionList {
        private val changedMetaFiles = HashSet<Path>()
        private val actions = mutableListOf<MetaAction>()

        private var nextGroupIdIndex = 0

        fun addInitialSetOfChangedMetaFiles(path: Path) {
            changedMetaFiles.add(path)
        }

        fun add(metaFile: Path, project:Project, action: () -> Unit) {
            if (changedMetaFiles.contains(metaFile)) return
            actions.add(MetaAction(metaFile, project, action))
        }

        fun isEmpty() = actions.isEmpty()

        fun execute() {
            val commandProcessor = CommandProcessor.getInstance()
            var groupId = commandProcessor.currentCommandGroupId
            if (groupId == null) {
                groupId = MetaGroupId(nextGroupIdIndex++)
                commandProcessor.currentCommandGroupId = groupId
            }
            application.invokeLater {
                commandProcessor.allowMergeGlobalCommands {
                    actions.forEach {
                        commandProcessor.executeCommand(it.project, {
                            application.runWriteAction {
                                it.execute()
                            }
                        }, getCommandName(), groupId)
                    }
                }
            }
        }

        @Nls
        fun getCommandName(): String {
            return if (actions.count() == 1)
                UnityPluginExplorerBundle.message("process.one.meta.file", actions.single().metaFile.name)
            else
                UnityPluginExplorerBundle.message("process.several.meta.files", actions.count())
        }
    }

    private class MetaAction(val metaFile: Path, val project:Project, private val action: () -> Unit) {
        fun execute() {
            try {
                action()
            }
            catch (ex: Throwable) {
                logger.error(ex)
            }
        }
    }

    private class MetaGroupId(val index: Int) {
        override fun toString() = "MetaGroupId$index"
    }
}