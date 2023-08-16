package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rd.ide.model.RdExistingSolution
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDescription
import com.jetbrains.rider.unity.UnityDetector

class UnityDetectorImpl(private val project: Project) : UnityDetector {
    override fun isUnitySolution(): Boolean
    {
        return UnityProjectDiscoverer.getInstance(project).isUnityProject
    }
}

@Service(Service.Level.PROJECT)
class UnityProjectDiscoverer(private val project: Project) : LifetimedService() {
    // It's a Unity project, but not necessarily loaded correctly (e.g. it might be opened as folder)
    val isUnityProjectFolder = hasUnityFileStructure(project)

    // These values will be false unless we've opened a .sln file. Note that the "sidecar" project is a solution that
    // lives in the same folder as generated unity project (not the same as a class library project, which could live
    // anywhere)
    val isUnityProject = isUnityProjectFolder && isCorrectlyLoadedSolution(project) && hasLibraryFolder(project)

    // Note that this will only return a sensible value once the solution + backend have finished loading
    val isUnityClassLibraryProject: Boolean?
        get() {
            val hasReference = project.solution.frontendBackendModel.hasUnityReference.valueOrNull ?: return null
            return hasReference && isCorrectlyLoadedSolution(project)
        }

    companion object {
        fun getInstance(project: Project): UnityProjectDiscoverer = project.service()

        fun hasUnityFileStructure(project: Project): Boolean {
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
}

fun Project.isUnityClassLibraryProject() = UnityProjectDiscoverer.getInstance(this).isUnityClassLibraryProject
fun Project.isUnityProject()= UnityProjectDiscoverer.getInstance(this).isUnityProject
fun Project.isUnityProjectFolder()= UnityProjectDiscoverer.getInstance(this).isUnityProjectFolder