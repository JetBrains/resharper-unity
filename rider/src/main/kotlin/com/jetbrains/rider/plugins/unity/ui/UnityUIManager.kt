package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.util.idea.tryGetComponent
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUIManager(private val unityProjectDiscoverer: UnityProjectDiscoverer,
                     solutionLifecycleHost: SolutionLifecycleHost,
                     project: Project)
    : LifetimedProjectComponent(project), PersistentStateComponent<Element> {

    companion object {
        const val hasMinimizedUiAttribute = "hasMinimizedUI"

        // TODO: When would this ever return null?
        fun tryGetInstance(project: Project): UnityUIManager? {
            return project.tryGetComponent()
        }
    }

    val hasMinimizedUi: Property<Boolean?> = Property(null) //null means undefined, default value

    init {
        solutionLifecycleHost.isBackendLoaded.whenTrue(componentLifetime) {
            // Only hide UI for generated projects, so that sidecar projects can still access nuget
            if (unityProjectDiscoverer.isUnityGeneratedProject && hasMinimizedUi.value == null) hasMinimizedUi.set(true)
        }
    }

    override fun getState(): Element? {
        val element = Element("state")
        val hasMinimizedUi = hasMinimizedUi.value
        element.setAttribute(hasMinimizedUiAttribute, hasMinimizedUi.toString())
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(hasMinimizedUiAttribute, "") ?: return
        if (!attributeValue.isEmpty()) {
            hasMinimizedUi.value = attributeValue.toBoolean()
        }
    }
}
