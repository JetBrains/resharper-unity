package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.IdeFrame
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.WindowManagerListener
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.util.idea.tryGetComponent
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.whenTrue
import org.jdom.Element

@State(name = "UnityProjectConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUIManager(private val unityProjectDiscoverer: UnityProjectDiscoverer,
                     private val host : UnityHost,
                     solutionLifecycleHost: SolutionLifecycleHost,
                     project: Project)
    : LifetimedProjectComponent(project), WindowManagerListener, PersistentStateComponent<Element> {

    companion object {
        const val hasMinimizedUiAttribute = "hasMinimizedUI"

        // TODO: When would this ever return null?
        fun tryGetInstance(project: Project): UnityUIManager? {
            return project.tryGetComponent()
        }
    }

    private val frameLifetime: SequentialLifetimes = SequentialLifetimes(componentLifetime)

    val hasMinimizedUi: Property<Boolean?> = Property(null) //null means undefined, default value

    init {
        WindowManager.getInstance().addListener(this)
        componentLifetime.onTermination { WindowManager.getInstance().removeListener(this) }
        solutionLifecycleHost.isBackendLoaded.whenTrue(componentLifetime) {
            // Only hide UI for generated projects, so that sidecar projects can still access nuget
            if (unityProjectDiscoverer.isLikeUnityGeneratedProject && hasMinimizedUi.value == null) hasMinimizedUi.set(true)
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
        if (frame.project == project && unityProjectDiscoverer.isLikeUnityProject) {
            frameLifetime.defineNext { _, frameLifetime ->
                installWidget(frame, frameLifetime)
            }
        }
    }

    private fun installWidget(frame: IdeFrame, lifetime: Lifetime) {
        val statusBar = frame.statusBar ?: return
        if (frame.statusBar.getWidget(UnityStatusBarIcon.StatusBarIconId) != null) {
            return
        }

        val iconWidget = UnityStatusBarIcon(host)
        host.unityState.advise(componentLifetime) { statusBar.updateWidget(iconWidget.ID()) }

        statusBar.addWidget(iconWidget, "after " + "ReadOnlyAttribute")

        lifetime.onTermination {
            if (frame.project != project) return@onTermination
            statusBar.removeWidget(iconWidget.ID())
        }
    }

    override fun beforeFrameReleased(frame: IdeFrame) {
        if (frame.project != project) return
        frameLifetime.terminateCurrent()
    }
}
