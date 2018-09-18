package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.IdeFrame
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.WindowManagerListener
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.idea.lifetime
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.whenTrue
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUIManager(private val unityReferenceDiscoverer: UnityReferenceDiscoverer,
                     private val host : UnityHost,
                     solutionLifecycleHost: SolutionLifecycleHost,
                     project: Project) : LifetimedProjectComponent(project), WindowManagerListener, PersistentStateComponent<Element> {

    companion object {
        const val hasMinimizedUiAttribute = "hasMinimizedUI"
    }

    private var frameLifetime: LifetimeDefinition? = null
    val hasMinimizedUi: Property<Boolean?> = Property(null) //null means undefined, default value

    init {
        WindowManager.getInstance().addListener(this)
        componentLifetime.add {
            WindowManager.getInstance().removeListener(this)
        }
        solutionLifecycleHost.isBackendLoaded.whenTrue(componentLifetime) {
            if (unityReferenceDiscoverer.isUnityGeneratedProject) {
                if(hasMinimizedUi.value == null)
                    hasMinimizedUi.set(true)
            }
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

    override fun frameCreated(frame: IdeFrame) {
        if (frame.project == project && unityReferenceDiscoverer.isUnityProject) {
            frameLifetime?.terminate()

            frameLifetime = Lifetime.create(project.lifetime)
            val frameLifetime = frameLifetime?.lifetime ?: error("frameLifetime was terminated from non-ui thread")
            installWidget(frame, frameLifetime)
        }
    }

    private fun installWidget(frame: IdeFrame, lifetime: Lifetime) {
        val statusBar = frame.statusBar ?: return
        if (frame.statusBar.getWidget(UnityStatusBarIcon.StatusBarIconId) != null) {
            return
        }

        val iconWidget = UnityStatusBarIcon(host)
        host.unityState.advise(componentLifetime){
            statusBar.updateWidget(iconWidget.ID())
        }

        statusBar.addWidget(iconWidget, "after " + "ReadOnlyAttribute")

        lifetime.add {
            if (frame.project != project) return@add
            statusBar.removeWidget(iconWidget.ID())
        }
    }

    override fun beforeFrameReleased(frame: IdeFrame) {
        if (frame.project != project) return
        frameLifetime?.terminate()
        frameLifetime = null
    }
}