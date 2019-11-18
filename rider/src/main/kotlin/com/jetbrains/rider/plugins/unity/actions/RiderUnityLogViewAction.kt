package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.PlatformDataKeys
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import javax.swing.Icon

abstract class RiderUnityLogViewAction(text:String, description:String, icon: Icon) : AnAction(text, description, icon) {
    override fun update(e: AnActionEvent) {
        e.presentation.apply {
            val dataContext = e.dataContext
            val tw = PlatformDataKeys.TOOL_WINDOW.getData(dataContext)
            isVisible = tw != null && tw.stripeTitle == UnityToolWindowFactory.TOOL_WINDOW_ID
        }
    }
}