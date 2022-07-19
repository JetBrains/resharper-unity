package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.project.Project
import com.intellij.util.getAttributeBooleanValue
import com.jetbrains.rider.util.idea.getService
import org.jdom.Element

@State(name = "UnityCheckinConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnsavedCheckinState(val project: Project) : PersistentStateComponent<Element> {

    companion object {
        fun getService(project: Project) = project.getService<UnsavedCheckinState>()
        const val attributeName = "checkUnsavedScenes"
    }

    var checkUnsavedState: Boolean = true

    override fun getState(): Element {
        val element = Element("state")
        element.setAttribute(attributeName, checkUnsavedState.toString())
        return element
    }

    override fun loadState(element: Element)  {
        val attributeValue = element.getAttributeBooleanValue(attributeName)
        checkUnsavedState = attributeValue
    }
}