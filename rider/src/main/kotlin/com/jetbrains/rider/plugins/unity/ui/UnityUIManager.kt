package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(value = "other.xml"))])
class UnityUIManager(private val unityReferenceDiscoverer: UnityReferenceDiscoverer, solutionLifecycleHost: SolutionLifecycleHost, project : Project) : LifetimedProjectComponent(project), PersistentStateComponent<Element> {

    companion object {
        const val isUnityProjectAttribute = "isUnityUI"
    }

    init {
        solutionLifecycleHost.isBackendLoaded.advise(componentLifetime) {
            if(it && isUnityUI == null && unityReferenceDiscoverer.isUnityProject){
                isUnityUI = true
            }
        }
    }

    private var isUnityUI : Boolean? = null

    override fun getState(): Element? {
        val element = Element("state")
        val value = isUnityUI ?: ""
        element.setAttribute(isUnityProjectAttribute, value.toString())
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(isUnityProjectAttribute, "") ?: return
        if(!attributeValue.isNullOrEmpty())
            isUnityUI = attributeValue.toBoolean()
    }
}