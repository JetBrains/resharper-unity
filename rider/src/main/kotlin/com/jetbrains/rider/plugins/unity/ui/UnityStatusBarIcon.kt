package com.jetbrains.rider.plugins.unity.ui

import com.intellij.execution.runners.ExecutionUtil
import com.intellij.icons.AllIcons
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.ui.AnimatedIcon
import com.intellij.ui.LayeredIcon
import com.intellij.util.Consumer
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.UnityHost.Companion.CONNECTED_IDLE
import com.jetbrains.rider.plugins.unity.UnityHost.Companion.CONNECTED_PLAY
import com.jetbrains.rider.plugins.unity.UnityHost.Companion.CONNECTED_REFRESH
import com.jetbrains.rider.plugins.unity.UnityHost.Companion.DISCONNECTED
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import java.awt.event.MouseEvent
import javax.swing.Icon

/**
 * @author Kirill.Skrygan
 */
class UnityStatusBarIcon(private val host: UnityHost): StatusBarWidget, StatusBarWidget.IconPresentation {
    companion object {
        const val StatusBarIconId = "UnityStatusIcon"
    }

    private val icon = UnityIcons.Status.UnityStatus
    private val connectedIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Icons.EditorConnectionStatus)
    private var myStatusBar: StatusBar? = null

    override fun ID(): String {
        return "UnityStatusIcon"
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
        if(host.sessionInitialized.value)
            return "Connected to Unity Editor"
        else
            return "No Unity Editor connection\nLoad the project in the Unity Editor to enable advanced functionality"
    }

    override fun getClickConsumer(): Consumer<MouseEvent>? {
        return Consumer {event ->
            if(event.button == MouseEvent.BUTTON1 || event.button == MouseEvent.BUTTON2)
                onClick(event)
        }
    }

    private fun onClick(e: MouseEvent) {
    }

    override fun getIcon(): Icon {
        when (host.unityState.value) {
            DISCONNECTED -> return icon
            CONNECTED_IDLE -> return connectedIcon
            CONNECTED_PLAY -> return LayeredIcon(connectedIcon, UnityIcons.Status.UnityStatusPlay)
            CONNECTED_REFRESH -> return LayeredIcon(connectedIcon, UnityIcons.Status.UnityStatusProgress)
        }

        return UnityIcons.Icons.AttachEditorDebugConfiguration
    }
}

