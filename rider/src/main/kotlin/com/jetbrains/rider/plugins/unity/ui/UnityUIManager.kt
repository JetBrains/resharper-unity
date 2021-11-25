package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.Property
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUIManager : PersistentStateComponent<Element> {

    companion object {
        const val hasMinimizedUiAttribute = "hasMinimizedUI"
        fun getInstance(project: Project): UnityUIManager =  project.service()
    }

    val hasMinimizedUi: Property<Boolean?> = Property(null) //null means undefined, default value

    override fun getState(): Element {
        val element = Element("state")
        val hasMinimizedUi = hasMinimizedUi.value
        element.setAttribute(hasMinimizedUiAttribute, hasMinimizedUi.toString())
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(hasMinimizedUiAttribute, "") ?: return
        if (attributeValue.isNotEmpty()) {
            hasMinimizedUi.value = attributeValue.toBoolean()
        }
    }
}
