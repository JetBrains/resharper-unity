package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.LifetimedProjectService
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUIManager(project: Project) : LifetimedProjectService(project), PersistentStateComponent<Element> {

    companion object {
        const val hasMinimizedUiAttribute = "hasMinimizedUI"
        fun getInstance(project: Project): UnityUIManager =  project.service()
    }

    val hasMinimizedUi: Property<Boolean?> = Property(null) //null means undefined, default value

    init {
        SolutionLifecycleHost.getInstance(project).isBackendLoaded.whenTrue(projectServiceLifetime) {
            // Only hide UI for generated projects, so that sidecar projects can still access nuget
            if (UnityProjectDiscoverer.getInstance(project).isUnityGeneratedProject && hasMinimizedUi.value == null) hasMinimizedUi.set(true)
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
