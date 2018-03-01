package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.IdeFrame
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.WindowManagerListener
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.lifetime.LifetimeDefinition
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(value = "other.xml"))])
class UnityUIManager(private val unityReferenceDiscoverer: UnityReferenceDiscoverer,
                     private val projectCustomDataHost : ProjectCustomDataHost,
                     solutionLifecycleHost: SolutionLifecycleHost,
                     project: Project) : LifetimedProjectComponent(project), PersistentStateComponent<Element>, WindowManagerListener {
    companion object {
        const val isUnityProjectAttribute = "isUnityUI"
    }

    init {
        WindowManager.getInstance().addListener(this)
        componentLifetime.add {
            WindowManager.getInstance().removeListener(this)
        }
        solutionLifecycleHost.isBackendLoaded.advise(componentLifetime) {
            if (it && isUnityUI == null && unityReferenceDiscoverer.isUnityProject) {
                isUnityUI = true
            }
        }


    }
    private var frameLifetime: LifetimeDefinition? = null
    private var isUnityUI: Boolean? = null

    override fun getState(): Element? {
        val element = Element("state")
        val value = isUnityUI ?: ""
        element.setAttribute(isUnityProjectAttribute, value.toString())
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(isUnityProjectAttribute, "") ?: return
        if (!attributeValue.isNullOrEmpty()) {
        }
        isUnityUI = attributeValue.toBoolean()
    }

    override fun frameCreated(frame: IdeFrame) {
        frameLifetime?.terminate()
        frameLifetime = Lifetime.create(componentLifetime)
        val frameLifetime = frameLifetime?.lifetime ?: error("frameLifetime was terminated from non-ui thread")
        installWidget(frame, frameLifetime)
    }

    private fun installWidget(frame: IdeFrame, lifetime: Lifetime) {
        val statusBar = frame.statusBar ?: return
        val iconWidget = UnityStatusBarIcon(projectCustomDataHost)

        projectCustomDataHost.unityState.advise(componentLifetime){
            statusBar.updateWidget(iconWidget.ID())
        }

        statusBar.addWidget(iconWidget, "after " + "ReadOnlyAttribute")
        lifetime.add {
            statusBar.removeWidget(iconWidget.ID())
        }
    }

    override fun beforeFrameReleased(frame: IdeFrame?) {
        if(frame?.project != project) return
        frameLifetime?.terminate()
        frameLifetime = null
    }
}