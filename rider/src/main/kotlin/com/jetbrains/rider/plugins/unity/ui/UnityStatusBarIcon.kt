package com.jetbrains.rider.plugins.unity.ui

import com.intellij.execution.runners.ExecutionUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.Consumer
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.unity.EditorState
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
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

    private val host = FrontendBackendHost.getInstance(project)

    init {
        host.unityState.advise(project.lifetime) {
            val statusBar = WindowManager.getInstance().getStatusBar(project)
            statusBar.updateWidget(StatusBarIconId)
        }
    }

    private val statusIcon = UnityIcons.Status.UnityStatus
    private val connectedIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatus)
    private val playIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatusPlay)
    private val pauseIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatusPause)
    private val progressIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Status.UnityStatusProgress)
    private var myStatusBar: StatusBar? = null

    override fun ID() = StatusBarIconId
    override fun getPresentation() = this

    override fun install(statusBar: StatusBar) {
        myStatusBar = statusBar
    }

    override fun dispose() {
        myStatusBar = null
    }

    @Suppress("DialogTitleCapitalization")
    override fun getTooltipText(): String {
        return when (host.unityState.valueOrDefault(EditorState.Disconnected)) {
            EditorState.Disconnected -> "No Unity Editor connection<br/>Load the project in the Unity Editor to enable advanced functionality"
            EditorState.Idle -> "Connected to Unity Editor"
            EditorState.Play -> "Connected to Unity Editor"
            EditorState.Pause -> "Connected to Unity Editor"
            EditorState.Refresh -> "Refreshing assets in Unity Editor"
        }
    }

    override fun getClickConsumer(): Consumer<MouseEvent>? = null

    override fun getIcon(): Icon {
        return when (host.unityState.valueOrDefault(EditorState.Disconnected)) {
            EditorState.Disconnected -> statusIcon
            EditorState.Idle -> connectedIcon
            EditorState.Play -> playIcon
            EditorState.Pause -> pauseIcon
            EditorState.Refresh -> progressIcon
        }
    }
}
