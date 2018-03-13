package com.jetbrains.rider.plugins.unity.ui

import com.intellij.execution.runners.ExecutionUtil
import com.intellij.icons.AllIcons
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.ui.AnimatedIcon
import com.intellij.ui.LayeredIcon
import com.intellij.util.Consumer
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost.Companion.CONNECTED_IDLE
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost.Companion.CONNECTED_PLAY
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost.Companion.CONNECTED_REFRESH
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost.Companion.DISCONNECTED
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import java.awt.event.MouseEvent
import javax.swing.Icon

/**
 * @author Kirill.Skrygan
 */
class UnityStatusBarIcon(private val projectCustomDataHost: ProjectCustomDataHost): StatusBarWidget, StatusBarWidget.IconPresentation {
    private var icon : Icon = UnityIcons.Logo
    private var myTooltip : String = ""
    private var myStatusBar: StatusBar? = null
    private val connectedIcon = ExecutionUtil.getLiveIndicator(UnityIcons.Logo)

    override fun ID(): String {
        return "UnityStatusIcon"
    }

    fun setActiveConnectionIcon() {
        icon = ExecutionUtil.getLiveIndicator(icon)
    }

    fun setDisconnectedIcon() {
        icon = UnityIcons.Logo
    }

    fun setTooltip(text: String) {
        myTooltip = text
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
        if(projectCustomDataHost.sessionInitialized.value)
            return "Rider and Unity Editor are connected with each other.\nTo enhance productivity some features will work through the Unity Editor"
        else
            return "No launched Unity Editor found.\nWith Unity Editor being launch, Rider will perform important actions via the Unity Editor."
    }

    override fun getClickConsumer(): Consumer<MouseEvent>? {
        return Consumer {event ->
            if(event.button == MouseEvent.BUTTON1 || event.button == MouseEvent.BUTTON2)
                onClick(event)
        }
    }

    fun onClick(e: MouseEvent) {

    }

    override fun getIcon(): Icon {
        when (projectCustomDataHost.unityState.value) {
            DISCONNECTED -> return UnityIcons.Logo
            CONNECTED_IDLE -> return connectedIcon
            CONNECTED_PLAY -> return LayeredIcon(connectedIcon, AllIcons.General.Run)
            CONNECTED_REFRESH -> return LayeredIcon(connectedIcon, AnimatedIcon.Grey())
        }

        return UnityIcons.Logo
    }
}

