package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.util.EventDispatcher
import com.jetbrains.rider.model.RdAssemblyReferenceDescriptor
import com.jetbrains.rider.model.RdProjectModelItemDescriptor
import com.jetbrains.rider.model.Solution
import com.jetbrains.rider.model.projectModelView
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.path
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rider.util.reactive.Property
import java.io.File

class UnityReferenceDiscoverer(project: Project) : LifetimedProjectComponent(project) {
    private val myProjectModelView = project.solution.projectModelView
    private val myEventDispatcher = EventDispatcher.create(UnityReferenceListener::class.java)
    var isUnityGeneratedProject = false
    var isUnityProject = false

    init {
        application.invokeLater {

            calcIsUnityProject()

            myProjectModelView.items.advise(componentLifetime) { item ->
                val itemData = item.newValueOpt
                if (itemData == null) {
                    // Item removed. Don't care about this. It's a weird scenario if someone
                    // removes a Unity project
                }
                else {
                    // Item added or updated
                    itemAddedOrUpdated(itemData.descriptor)
                }
            }
        }
    }

    private fun itemAddedOrUpdated(descriptor: RdProjectModelItemDescriptor) {
        if (descriptor is RdAssemblyReferenceDescriptor && descriptor.name == "UnityEngine") {
            myEventDispatcher.multicaster.hasUnityReference()
            calcIsUnityProject()
        }
    }

    private fun calcIsUnityProject() {
        isUnityProject = hasAssetsFolder(project)
        isUnityGeneratedProject = hasAssetsFolder(project) && isUnityGeneratedSolutionName(project.solution)
    }

    private fun isUnityGeneratedSolutionName(solution: Solution): Boolean {
        return File(solution.path).nameWithoutExtension == File(File(solution.path).parent).name
    }

    fun addUnityReferenceListener(listener: UnityReferenceListener) {
        myEventDispatcher.addListener(listener)
    }

    companion object {
        fun hasAssetsFolder (project:Project):Boolean {
            val assetsFolder = project.baseDir?.findChild("Assets")
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
