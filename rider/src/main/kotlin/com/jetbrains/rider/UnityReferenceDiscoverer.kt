package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.util.EventDispatcher
import com.jetbrains.rider.model.RdAssemblyReferenceDescriptor
import com.jetbrains.rider.model.RdExistingSolution
import com.jetbrains.rider.model.RdProjectModelItemDescriptor
import com.jetbrains.rider.model.projectModelView
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDescription
import com.jetbrains.rider.projectView.solutionFile
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rider.util.reactive.Property

class UnityReferenceDiscoverer(project: Project) : LifetimedProjectComponent(project) {
    private val myProjectModelView = project.solution.projectModelView
    private val myEventDispatcher = EventDispatcher.create(UnityReferenceListener::class.java)

    val isUnityProjectFolder = hasUnityFolders(project)

    // These values will be false unless we've opened a .sln file. Note that the "sidecar" project is a solution that
    // lives in the same folder as generated unity project
    val isUnitySidecarProject = isUnityProjectFolder && isCorrectlyLoadedSolution(project) && !solutionNameMatchesUnityProjectName(project)
    val isUnityGeneratedProject = isUnityProjectFolder && isCorrectlyLoadedSolution(project) && solutionNameMatchesUnityProjectName(project)

    // TODO: This isn't used anywhere
    var hasReferenceToUnityProject = false

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

    private fun itemAddedOrUpdated(descriptor: RdProjectModelItemDescriptor) {
        if (descriptor is RdAssemblyReferenceDescriptor && descriptor.name == "UnityEngine") {
            myEventDispatcher.multicaster.hasUnityReference()
            hasReferenceToUnityProject = true
        }
    }

    // Returns false when opening a Unity project as a plain folder
    private fun isCorrectlyLoadedSolution(project: Project): Boolean {
        val solutionFile = project.solutionFile
        return project.solutionDescription is RdExistingSolution && solutionFile.isFile && solutionFile.extension.equals("sln", true)
    }

    private fun solutionNameMatchesUnityProjectName(project: Project): Boolean {
        val solutionFile = project.solutionFile
        return solutionFile.nameWithoutExtension == project.baseDir.name
    }

    fun addUnityReferenceListener(listener: UnityReferenceListener) {
        myEventDispatcher.addListener(listener)
    }

    companion object {
        private fun hasUnityFolders (project:Project):Boolean =
                hasAssetsFolder(project) && hasLibraryFolder(project) && hasProjectSettingsFolder(project)

        private fun hasAssetsFolder (project:Project):Boolean =
                project.baseDir?.findChild("Assets")?.isDirectory == true

        private fun hasLibraryFolder (project:Project):Boolean =
                project.baseDir?.findChild("Library")?.isDirectory == true

        private fun hasProjectSettingsFolder (project:Project):Boolean =
                project.baseDir?.findChild("ProjectSettings")?.isDirectory == true
    }
}

fun Project.isUnityGeneratedProject(): Boolean {
    val referenceDiscoverer = this.getComponent<UnityReferenceDiscoverer>()
    return referenceDiscoverer.isUnityGeneratedProject
}

// Lives in the same folder as a normal Unity project, but isn't the generated one
fun Project.isUnitySidecarProject(): Boolean {
    val referenceDiscoverer = this.getComponent<UnityReferenceDiscoverer>()
    return referenceDiscoverer.isUnitySidecarProject
}

fun Project.isConnectedToEditor(): Boolean {
    val component = this.getComponent<UnityHost>()
    return component.sessionInitialized.value
}

fun Project.isConnectedToEditorLive(): Property<Boolean> {
    val component = this.getComponent<UnityHost>()
    return component.sessionInitialized
}
