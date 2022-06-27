package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidgetFactory
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ShaderWidgetFactory: StatusBarWidgetFactory {
    override fun getId() = "ShaderWidget"
    override fun isAvailable(project: Project) = UnityProjectDiscoverer.getInstance(project).isUnityProject
    override fun canBeEnabledOn(statusBar: StatusBar) = true
    override fun getDisplayName() = UnityUIBundle.message("unity.shader.file.context")
    override fun disposeWidget(widget: StatusBarWidget) {}
    override fun createWidget(project: Project) = ShaderWidget(project)
}