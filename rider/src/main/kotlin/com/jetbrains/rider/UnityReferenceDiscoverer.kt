package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.util.EventDispatcher
import com.jetbrains.rider.model.Solution
import com.jetbrains.rider.model.projectModelView
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.ui.UnityUIManager
import com.jetbrains.rider.projectView.path
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rider.util.idea.tryGetComponent
import com.jetbrains.rider.util.reactive.Property
import java.io.File

class UnityReferenceDiscoverer(project: Project) : LifetimedProjectComponent(project) {
    private val myEventDispatcher = EventDispatcher.create(UnityReferenceListener::class.java)
    var isUnityGeneratedProject = false
    var isUnityProject = false

    init {
        isUnityProject = hasUnityFolders(project) && generatedSolutionFileExistsNear(project.solution)
        isUnityGeneratedProject = hasAssetsFolder(project) && isUnityGeneratedSolutionName(project.solution)
    }

    private fun generatedSolutionFileExistsNear(solution: Solution): Boolean {
        var dirPath = File(solution.path).toPath().parent
        var expectedGeneratedSolutionName = dirPath.toFile().name+".sln"
        var expectedGneratedSolutionFile = dirPath!!.resolve(expectedGeneratedSolutionName).toFile()
        return expectedGneratedSolutionFile.exists()
    }

    private fun isUnityGeneratedSolutionName(solution: Solution): Boolean {
        return File(solution.path).nameWithoutExtension == File(File(solution.path).parent).name
    }

    fun addUnityReferenceListener(listener: UnityReferenceListener) {
        myEventDispatcher.addListener(listener)
    }

    companion object {
        fun hasUnityFolders (project:Project):Boolean {
            return hasAssetsFolder(project) && hasLibraryFolder(project) && hasProjectSettingsFolder(project);
        }
        fun hasAssetsFolder (project:Project):Boolean {
            val assetsFolder = project.baseDir?.findChild("Assets")
            return assetsFolder != null
        }
        fun hasLibraryFolder (project:Project):Boolean {
            val assetsFolder = project.baseDir?.findChild("Library")
            return assetsFolder != null
        }
        fun hasProjectSettingsFolder (project:Project):Boolean {
            val assetsFolder = project.baseDir?.findChild("ProjectSettings")
            return assetsFolder != null
        }
    }
}

fun Project.isUnityGeneratedProject(): Boolean {
    val component = this.getComponent<UnityReferenceDiscoverer>()
    return component.isUnityGeneratedProject
}

fun Project.isConnectedToEditor(): Boolean {
    val component = this.getComponent<UnityHost>()
    return component.sessionInitialized.value
}

fun Project.isConnectedToEditorLive(): Property<Boolean> {
    val component = this.getComponent<UnityHost>()
    return component.sessionInitialized
}
