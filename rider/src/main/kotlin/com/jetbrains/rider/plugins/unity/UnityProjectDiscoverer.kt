package com.jetbrains.rider.plugins.unity

import com.intellij.ide.projectView.ProjectView
import com.intellij.openapi.application.EDT
import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.BaseState
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.SimplePersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rd.framework.impl.RdProperty
import com.jetbrains.rd.ide.model.RdExistingSolution
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rd.util.threading.coroutines.launch
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solutionDescription
import com.jetbrains.rider.projectView.views.fileSystemExplorer.FileSystemExplorerPane
import com.jetbrains.rider.unity.UnityDetector
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class UnityDetectorImpl(private val project: Project) : UnityDetector {

    override val isUnitySolution: Property<Boolean>
        get() = UnityProjectDiscoverer.getInstance(project).isUnityProject
}

@Service(Service.Level.PROJECT)
class UnityProjectDiscoverer(val project: Project) {
    // These values will be false unless we've opened a .sln file. Note that the "sidecar" project is a solution that
    // lives in the same folder as generated unity project (not the same as a class library project, which could live
    // anywhere)
    val isUnityProject = Property(UnityProjectDiscovererState.getInstance(project).isUnityProjectState)

    // It's a Unity project, but not necessarily loaded correctly (e.g. it might be opened as folder)
    val isUnityProjectFolder = RdProperty(UnityProjectDiscovererState.getInstance(project).isUnityProjectFolderState)
    val hasUnityReference = RdProperty(UnityProjectDiscovererState.getInstance(project).hasUnityReferenceState)

    init {
        // we can't change this to ProjectActivity because all of them are executes synchronously in tests
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        lifetime.launch {
            val hasUnityFileStructure = hasUnityFileStructure(project)
            isUnityProjectFolder.set(hasUnityFileStructure)
            UnityProjectDiscovererState.getInstance(project).isUnityProjectFolderState = hasUnityFileStructure

            val isUnityProjectVal = hasUnityFileStructure && isCorrectlyLoadedSolution(project) && hasLibraryFolder(project)

            val oldVal = isUnityProject.value // old val is the one from the settings
            withContext(Dispatchers.EDT) {
                isUnityProject.set(isUnityProjectVal)
            }
            UnityProjectDiscovererState.getInstance(project).isUnityProjectState = isUnityProjectVal

            // this only happens for the first opening of a Unity project, later the cached value is the same as evaluated one
            if (oldVal != isUnityProjectVal) {
                val projectView = ProjectView.getInstance(project)
                withContext(Dispatchers.EDT) {
                    val fileSystemPane = projectView.getProjectViewPaneById(FileSystemExplorerPane.ID)
                    // if fileSystemPane is null, means that ProjectView is not yet initialized, so we can do nothing
                    if (fileSystemPane != null) {
                        val pane = projectView.getProjectViewPaneById(UnityExplorer.ID)
                        if (isUnityProjectVal) {
                            if (pane == null) projectView.addProjectPane(UnityExplorer(project))
                            projectView.changeView(UnityExplorer.ID)
                        }
                        else if (pane != null) {
                            projectView.removeProjectPane(pane)
                        }
                    }
                }
            }
        }
    }

    // When Unity has generated sln, Library folder was also created. Lets' be more strict and check it.
    private fun hasLibraryFolder(project: Project): Boolean {
        val projectDir = project.projectDir
        return projectDir.findChild("Library")?.isDirectory != false
    }

    // Returns false when opening a Unity project as a plain folder
    private fun isCorrectlyLoadedSolution(project: Project): Boolean {
        return project.solutionDescription is RdExistingSolution
    }

    companion object {
        fun getInstance(project: Project): UnityProjectDiscoverer = project.service()

        private fun hasUnityFileStructure(project: Project): Boolean {
            // projectDir will fail with the default project
            return !project.isDefault && hasUnityFileStructure(project.projectDir)
        }

        fun hasUnityFileStructure(projectDir: VirtualFile): Boolean {
            // Make sure we have an Assets folder and a ProjectSettings folder. We can't rely on Library, as that won't
            // be available for a freshly checked out project. That's quite a weak check, so to reduce the chance of
            // false positives, we'll also check for `ProjectSettings/ProjectVersion.txt` OR `ProjectSettings/*.asset`.
            // Some people don't check ProjectVersion.txt into source control, as that means that all team members have
            // to be on the same version of Unity, even the same patch version (although this is probably a good idea).
            // (Technically, Library will be there for a generated project, as we won't have project files without the
            // project being loaded into Unity, which will create the Library folder. But it won't be there for sidecar
            // projects or if the project is accidentally opened as a folder)
            if (projectDir.findChild("Assets")?.isDirectory == false)
                return false
            val projectSettings = projectDir.findChild("ProjectSettings")
            if (projectSettings == null || !projectSettings.isDirectory)
                return false
            return projectSettings.children.any {
                it.name == "ProjectVersion.txt" || it.extension == "asset"
            }
        }

        fun searchUpForFolderWithUnityFileStructure(file: VirtualFile, maxSteps: Int = 10): Pair<Boolean, VirtualFile?> {
            var dir: VirtualFile? = file
            repeat(maxSteps) {
                if (dir == null) return@repeat

                dir?.let {
                    if ((it.name == "Assets" || it.name == "Packages") && hasUnityFileStructure(
                            it.parent)) {
                        return Pair(true, it.parent)
                    }
                    dir = it.parent
                }
            }
            return Pair(false, null)
        }
    }

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.hasUnityReference.adviseUntil(lifetime) {
                getInstance(session.project).hasUnityReference.set(it)
                UnityProjectDiscovererState.getInstance(session.project).hasUnityReferenceState = it
                it
            }
        }
    }
}

val Project.hasUnityReference: RdProperty<Boolean>
    get() = UnityProjectDiscoverer.getInstance(this).hasUnityReference
val Project.isUnityProject: Property<Boolean>
    get() = UnityProjectDiscoverer.getInstance(this).isUnityProject
val Project.isUnityProjectFolder: RdProperty<Boolean>
    get() = UnityProjectDiscoverer.getInstance(this).isUnityProjectFolder

@Service(Service.Level.PROJECT)
@State(name = "UnityProjectDiscoverer", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
internal class UnityProjectDiscovererState : SimplePersistentStateComponent<UnityProjectDiscovererState.State>(State()) {
    companion object {
        fun getInstance(project: Project): UnityProjectDiscovererState = project.service()
    }

    class State : BaseState() {
        var isUnityProject by property(false)
        var isUnityProjectFolder by property(false)
        var hasUnityReference by property(false)
    }

    var isUnityProjectState: Boolean
        get() = state.isUnityProject
        set(value) {
            state.isUnityProject = value
        }

    var isUnityProjectFolderState: Boolean
        get() = state.isUnityProjectFolder
        set(value) {
            state.isUnityProjectFolder = value
        }

    var hasUnityReferenceState: Boolean
        get() = state.hasUnityReference
        set(value) {
            state.hasUnityReference = value
        }
}