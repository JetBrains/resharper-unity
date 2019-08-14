package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidgetProvider
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityHost

class UnityStatusBarWidget: StatusBarWidgetProvider {
    override fun getWidget(project: Project): StatusBarWidget? {
        if (!UnityProjectDiscoverer.getInstance(project).isUnityProject)
            return null
        return UnityStatusBarIcon(UnityHost.getInstance(project))
    }

    override fun getAnchor(): String {
        return StatusBar.Anchors.after(StatusBar.StandardWidgets.READONLY_ATTRIBUTE_PANEL)
    }
}