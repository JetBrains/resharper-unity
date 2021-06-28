@file:Suppress("DEPRECATION")

package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidgetFactory
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer

class UnityStatusBarWidget: StatusBarWidgetFactory {
    override fun getId() = UnityStatusBarIcon.StatusBarIconId
    override fun isAvailable(project: Project) = UnityProjectDiscoverer.getInstance(project).isUnityProject
    override fun canBeEnabledOn(statusBar: StatusBar) = true
    override fun getDisplayName() = "Unity Editor connection"
    override fun disposeWidget(widget: StatusBarWidget) {}
    override fun createWidget(project: Project) = UnityStatusBarIcon(project)
}