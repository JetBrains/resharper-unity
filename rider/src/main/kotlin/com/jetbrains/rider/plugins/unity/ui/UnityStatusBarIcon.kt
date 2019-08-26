package com.jetbrains.rider.plugins.unity.ui

import com.intellij.execution.runners.ExecutionUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.Consumer
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.EditorState
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.util.idea.lifetime
import icons.UnityIcons
import java.awt.event.MouseEvent
import javax.swing.Icon

/**
 * @author Kirill.Skrygan
 */
class UnityStatusBarIcon(project: Project): StatusBarWidget, StatusBarWidget.IconPresentation {
    companion object {
        const val StatusBarIconId = "UnityStatusIcon"
    }

    private val host = UnityHost.getInstance(project)

    init {
        host.unityState.advise(project.lifetime) {
            val statusBar = WindowManager.getInstance().getStatusBar(project)
            statusBar.updateWidget(StatusBarIconId)
        }
    }

    private val statusIcon = UnityIcons.Status.UnityStatus
    private val connectedIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatus)
    private val playIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatusPlay)
    private val progressIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatusProgress)
    private var myStatusBar: StatusBar? = null

    override fun ID(): String {
        return StatusBarIconId
    }

    override fun getPresentation(type: StatusBarWidget.PlatformType): StatusBarWidget.WidgetPresentation? {
        return this
    }

    override fun install(statusBar: StatusBar) {
        myStatusBar = statusBar
    }

    override fun dispose() {
        myStatusBar = null
    }

    override fun getTooltipText(): String? {
        return if(host.sessionInitialized.valueOrDefault(false))
            "Connected to Unity Editor"
        else
            "No Unity Editor connection\nLoad the project in the Unity Editor to enable advanced functionality"
    }

    override fun getClickConsumer(): Consumer<MouseEvent>? = null

    override fun getIcon(): Icon {
        return when (host.unityState.valueOrDefault(EditorState.Disconnected)) {
            EditorState.Disconnected -> statusIcon
            EditorState.ConnectedIdle -> connectedIcon
            EditorState.ConnectedPlay -> playIcon
            EditorState.ConnectedRefresh -> progressIcon
        }
    }
}
