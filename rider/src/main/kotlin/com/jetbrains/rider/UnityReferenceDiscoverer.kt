package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.util.EventDispatcher
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.RdAssemblyReferenceDescriptor
import com.jetbrains.rider.model.RdExistingSolution
import com.jetbrains.rider.model.RdProjectModelItemDescriptor
import com.jetbrains.rider.model.projectModelView
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDescription
import com.jetbrains.rider.projectView.solutionFile
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rider.util.reactive.valueOrDefault

class UnityReferenceDiscoverer(project: Project) : LifetimedProjectComponent(project) {
    private val myProjectModelView = project.solution.projectModelView
    private val myEventDispatcher = EventDispatcher.create(UnityReferenceListener::class.java)

    val isUnityLikeProjectFolder = hasAssetsAndProjectSettingsFolders(project) // projects in vcs don't usually have Library folder, we want to show a OpenUnityProjectAsFolderNotification for them
    private val isUnityProjectFolder = hasUnityFolders(project)

    // These values will be false unless we've opened a .sln file. Note that the "sidecar" project is a solution that
    // lives in the same folder as generated unity project
    val isUnityProject = isUnityProjectFolder && isCorrectlyLoadedSolution(project)
    val isUnityGeneratedProject = isUnityProjectFolder && isCorrectlyLoadedSolution(project) && solutionNameMatchesUnityProjectName(project)
    val isUnitySidecarProject = isUnityProjectFolder && isCorrectlyLoadedSolution(project) && !solutionNameMatchesUnityProjectName(project)

    init {
        application.invokeLater {
            myProjectModelView.items.advise(componentLifetime) { item ->
                val itemData = item.newValueOpt
                if (itemData == null) {
                    // Item removed. Don't care about this. It's a weird scenario if someone
                    // removes a Unity project
                } else {
                    // Item added or updated
                    itemAddedOrUpdated(itemData.descriptor)
                }
            }
        }
    }

    companion object {
        fun getInstance(project: Project) = project.getComponent<UnityReferenceDiscoverer>()

        private fun hasAssetsAndProjectSettingsFolders(project:Project):Boolean =
                hasAssetsFolder(project) && hasProjectSettingsFolder(project)

        private fun hasUnityFolders (project:Project):Boolean =
                hasAssetsFolder(project) && hasLibraryFolder(project) && hasProjectSettingsFolder(project)

        private fun hasAssetsFolder (project:Project):Boolean =
                project.projectDir.findChild("Assets")?.isDirectory == true

        private fun hasLibraryFolder (project:Project):Boolean =
                project.projectDir.findChild("Library")?.isDirectory == true

        private fun hasProjectSettingsFolder (project:Project):Boolean =
                project.projectDir.findChild("ProjectSettings")?.isDirectory == true
    }

    private fun itemAddedOrUpdated(descriptor: RdProjectModelItemDescriptor) {
        if (descriptor is RdAssemblyReferenceDescriptor && descriptor.name == "UnityEngine") {
            myEventDispatcher.multicaster.hasUnityReference()
        }
    }

    // Returns false when opening a Unity project as a plain folder
    private fun isCorrectlyLoadedSolution(project: Project): Boolean {
        val solutionFile = project.solutionFile
        return project.solutionDescription is RdExistingSolution && solutionFile.isFile && solutionFile.extension.equals("sln", true)
    }

    private fun solutionNameMatchesUnityProjectName(project: Project): Boolean {
        val solutionFile = project.solutionFile
        return solutionFile.nameWithoutExtension == project.projectDir.name
    }

    fun addUnityReferenceListener(listener: UnityReferenceListener) {
        myEventDispatcher.addListener(listener)
    }
}

fun Project.isUnityGeneratedProject() = UnityReferenceDiscoverer.getInstance(this).isUnityGeneratedProject

// Lives in the same folder as a normal Unity project, but isn't the generated one
fun Project.isUnitySidecarProject()= UnityReferenceDiscoverer.getInstance(this).isUnitySidecarProject

fun Project.isUnityProject()= UnityReferenceDiscoverer.getInstance(this).isUnityProject

fun Project.isConnectedToEditor()= UnityHost.getInstance(this).sessionInitialized.valueOrDefault(false)
