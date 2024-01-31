package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.Property
import org.jdom.Element

@Service(Service.Level.PROJECT)
@State(name = "UnityProjectConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUIManager : PersistentStateComponent<Element> {

    companion object {
        const val hasMinimizedUiAttribute = "hasMinimizedUI"
        const val hasHiddenPlayButtonsAttribute = "hasHiddenPlayButtons"
        fun getInstance(project: Project): UnityUIManager = project.service()
    }

    val hasMinimizedUi: Property<Boolean?> = Property(null) //null means undefined, default value
    val hasHiddenPlayButtons: Property<Boolean?> = Property(null) //null means undefined, default value

    override fun getState(): Element {
        val element = Element("state")
        val hasMinimizedUi = hasMinimizedUi.value
        if (hasMinimizedUi != null)
            element.setAttribute(hasMinimizedUiAttribute, hasMinimizedUi.toString())

        val hasHiddenPlayButtons = hasHiddenPlayButtons.value
        if (hasHiddenPlayButtons != null)
            element.setAttribute(hasHiddenPlayButtonsAttribute, hasHiddenPlayButtons.toString())

        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(hasMinimizedUiAttribute, "")
        if (attributeValue != null && attributeValue.isNotEmpty()) {
            hasMinimizedUi.value = attributeValue.toBoolean()
        }

        val attributePlayButtonsValue = element.getAttributeValue(hasHiddenPlayButtonsAttribute, "")
        if (attributePlayButtonsValue != null && attributePlayButtonsValue.isNotEmpty()) {
            hasHiddenPlayButtons.value = attributePlayButtonsValue.toBoolean()
        }
    }
}

fun Property<Boolean?>.hasTrueValue(): Boolean {
    return this.value != null && this.value!!
}