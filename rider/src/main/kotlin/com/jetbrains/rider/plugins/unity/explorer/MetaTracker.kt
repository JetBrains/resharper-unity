package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.Disposable
import com.intellij.openapi.command.CommandEvent
import com.intellij.openapi.command.CommandListener
import com.intellij.openapi.command.CommandProcessor
import com.intellij.openapi.command.undo.UndoManager
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.findFile
import com.intellij.openapi.vfs.newvfs.BulkFileListener
import com.intellij.openapi.vfs.newvfs.events.*
import com.intellij.util.PathUtil
import com.intellij.util.application
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rd.platform.util.getLogger
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.addUnique
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

class MetaTrackerInitializer : ProjectActivity {
    override suspend fun execute(project: Project) {
        if (project.isUnityProjectFolder()){
            MetaTracker.getInstance().register(project)
        }
    }
}

@Service(Service.Level.APP)
class MetaTracker {

    private val lock = Object()
    private var projects = mutableSetOf<Project>()
    companion object {
        fun getInstance() = service<MetaTracker>()
        private val logger = getLogger<MetaTracker>()
    }

    fun register(project: Project) {
        project.lifetime.bracketIfAlive({ synchronized(lock) { projects.add(project) } },
                                        { synchronized(lock) { projects.remove(project) } })
    }

    private val actionsPerProject = mutableMapOf<Project, MetaActionList>()

    fun onEvent(events: MutableList<out VFileEvent>) {

        val unityProjects = synchronized(lock) { mutableListOf<Project>().also { it.addAll(projects) } }.filter { !isUndoRedoInProgress(it) && !it.isDisposed }.toList()

        for (event in events) {
            if (!isValidEvent(event)) continue
            for (project in unityProjects) {
                if (isApplicableForProject(event, project)) {
                    val actions = getOrCreate(project)
                    if (isMetaFile(event)) // Collect modified meta files at first (LocalHistory or git or something else)
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
                                        if (shouldCreateMetaFile(project, event.file, event.parent)) createMetaFile(event.parent, metaFileName, ls)
                                    }
                                }
                                is VFileDeleteEvent -> {
                                    val metaFile = getMetaFile(event.path) ?: continue
                                    actions.add(metaFile, project) {
                                        VfsUtil.findFile(metaFile, true)?.delete(this)
                                    }
                                }
                                is VFileCopyEvent -> {
                                    val metaFile = getMetaFile(event.file.path) ?: continue
                                    val ls = event.file.detectedLineSeparator ?: "\n"
                                    actions.add(metaFile, project) {
                                        if (shouldCreateMetaFile(project, event.file, event.newParent))
                                            createMetaFile(event.newParent, getMetaFileName(event.newChildName), ls)
                                    }
                                }
                                is VFileMoveEvent -> {
                                    val metaFile = getMetaFile(event.oldPath) ?: continue
                                    actions.add(metaFile, project) { VfsUtil.findFile(metaFile, true)?.move(this, event.newParent)
                                    }
                                }
                                is VFilePropertyChangeEvent -> {
                                    if (!event.isRename) continue
                                    val metaFile = getMetaFile(event.oldPath) ?: continue
                                    actions.add(metaFile, project) {
                                        val target = getMetaFileName(event.newValue as String)
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
                        }
                        catch (t: Throwable) {
                            logger.error(t)
                            continue
                        }
                    }
                }
            }
        }
    }

    private fun getOrCreate(project: Project): MetaActionList {
        var actions = actionsPerProject[project]
        if (actions == null) {
            actions = MetaActionList(project)
            actionsPerProject.addUnique(project.lifetime, project, actions)
        }
        return actions
    }

    private fun isValidEvent(event: VFileEvent): Boolean {
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

    private fun getSourceRoots(project:Project): MutableSet<VirtualFile> {
        val roots = mutableSetOf<VirtualFile>()
        val assets = project.projectDir.findChild("Assets") ?: return roots
        roots.add(assets)
        val editablePackages = WorkspaceModel.getInstance(project).getPackages().filter { it.isEditable() }.mapNotNull { it.packageFolder }
        roots.addAll(editablePackages)
        return roots
    }

    private fun isApplicableForProject(event: VFileEvent, project: Project): Boolean {
        val file = event.file ?: return false

        for (folder in getSourceRoots(project)) {
            if (VfsUtil.isAncestor(folder, file, false)) return true
        }

        return false
    }

    private fun getMetaFile(path: String?): Path? {
        path ?: return null
        val file = Paths.get(path)
        val metaFileName = getMetaFileName(file.name)
        return file.parent.resolve(metaFileName)
    }

    private fun getMetaFile(file: VirtualFile?): VirtualFile? {
        file ?: return null
        val metaFileName = getMetaFileName(file.name)
        return file.parent.findFile(metaFileName)
    }

    private fun getMetaFileName(fileName: String) = "$fileName.meta"

    private fun shouldCreateMetaFile(project: Project, assetFile: VirtualFile?, parent: VirtualFile):Boolean {
        // avoid adding a meta file for:
        // a hidden Asset (like `Documentation~`), but not its children
        // if parent folder (except SourceRoots) doesn't have meta file, this would cover children of the HiddenAssetFolder, see RIDER-93037

        val roots = getSourceRoots(project)
        if (UnityExplorerFileSystemNode.isHiddenAsset(assetFile) || (!roots.contains(parent) && getMetaFile(parent) == null)) {
            logger.info("avoid adding meta file for $assetFile.")
            return false
        }

        return true
    }

    private fun createMetaFile(parent: VirtualFile, metaFileName: String, ls: String) {
        val metaFile = parent.createChildData(this, metaFileName)
        val guid = UUID.randomUUID().toString().replace("-", "").substring(0, 32)
        val timestamp = LocalDateTime.now(ZoneOffset.UTC).atZone(ZoneOffset.UTC).toEpochSecond() // LocalDateTime to epoch seconds
        val content = "fileFormatVersion: 2${ls}guid: ${guid}${ls}timeCreated: $timestamp"
        VfsUtil.saveText(metaFile, content)
    }

    private class MetaActionList(project: Project) {

        init {
            val connection = project.messageBus.connect(project.lifetime.createNestedDisposable())
            connection.subscribe(CommandListener.TOPIC, object : CommandListener {
                override fun beforeCommandFinished(event: CommandEvent) {
                    // apply all changes from Map<Runnable, List<Path>> and add our changes to meta files

                    execute(event)
                    clear()

                    super.beforeCommandFinished(event)
                }
            })
        }

        private fun clear() {
            application.assertIsDispatchThread()
            changedMetaFiles.clear()
            actions.clear()
        }

        private val changedMetaFiles = HashSet<Path>()
        private val actions = mutableListOf<MetaAction>()

        private var nextGroupIdIndex = 0

        fun addInitialSetOfChangedMetaFiles(path: Path) {
            changedMetaFiles.add(path)
        }

        fun add(metaFile: Path, project: Project, action: () -> Unit) {
            if (changedMetaFiles.contains(metaFile)) return
            actions.add(MetaAction(metaFile, project, action))
        }

        fun execute(event: CommandEvent) {
            if (actions.isEmpty()) return

            val commandProcessor = CommandProcessor.getInstance()
            var groupId = commandProcessor.currentCommandGroupId
            if (groupId == null) {
                groupId = MetaGroupId(nextGroupIdIndex++)
                commandProcessor.currentCommandGroupId = groupId
            }

            commandProcessor.allowMergeGlobalCommands {
                actions.forEach {
                    commandProcessor.executeCommand(it.project, {
                        application.runWriteAction {
                            if (!changedMetaFiles.contains(it.metaFile)) // the meta file got restored by LocalHistory or git or maybe undo
                                it.execute()
                        }
                    }, getCommandName(), groupId)
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

    private class MetaAction(val metaFile: Path, val project: Project, private val action: () -> Unit) {
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

class MetaTrackerListener: BulkFileListener, VfsBackendRequester, Disposable {
    override fun after(events: MutableList<out VFileEvent>) {
        MetaTracker.getInstance().onEvent(events)
    }
    override fun dispose() = Unit
}