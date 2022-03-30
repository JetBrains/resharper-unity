package com.jetbrains.rider.plugins.unity.ui

import com.intellij.execution.runners.ExecutionUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.Consumer
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
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
        host.model.unityEditorState.advise(project.lifetime) {
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
        return when (host.model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected)) {
            UnityEditorState.Disconnected -> UnityUIBundle.message("tooltip.no.unity.editor.connection")
            UnityEditorState.Idle -> UnityUIBundle.message("tooltip.connected.to.unity.editor")
            UnityEditorState.Play -> UnityUIBundle.message("tooltip.connected.to.unity.editor")
            UnityEditorState.Pause -> UnityUIBundle.message("tooltip.connected.to.unity.editor")
            UnityEditorState.Refresh -> UnityUIBundle.message("tooltip.refreshing.assets.in.unity.editor")
        }
    }

    override fun getClickConsumer(): Consumer<MouseEvent>? = null

    override fun getIcon(): Icon {
        return when (host.model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected)) {
            UnityEditorState.Disconnected -> statusIcon
            UnityEditorState.Idle -> connectedIcon
            UnityEditorState.Play -> playIcon
            UnityEditorState.Pause -> pauseIcon
            UnityEditorState.Refresh -> progressIcon
        }
    }
}
