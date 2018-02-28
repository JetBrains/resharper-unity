package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.IdeFrame
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.WindowManagerListener
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(value = "other.xml"))])
class UnityShellCustomizer(project : Project) : LifetimedProjectComponent(project), WindowManagerListener, PersistentStateComponent<Element> {

    companion object {
        const val isUnityProjectAttribute = "isUnityProject"
    }

    private var isUnityProject : Boolean = false

    override fun getState(): Element? {
        val element = Element("state")
        element.setAttribute(isUnityProjectAttribute, isUnityProject.toString())
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(isUnityProjectAttribute) ?: return
        isUnityProject = attributeValue.toBoolean()
    }

    init {
        WindowManager.getInstance().addListener(this)
        componentLifetime.add {
            WindowManager.getInstance().removeListener(this)
        }
    }

    override fun frameCreated(frame: IdeFrame?) {

    }

    override fun beforeFrameReleased(frame: IdeFrame?) {

    }
}