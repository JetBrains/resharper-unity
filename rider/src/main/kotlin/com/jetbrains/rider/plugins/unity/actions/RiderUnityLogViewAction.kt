package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.isUnityProject

abstract class RiderUnityLogViewAction : DumbAwareAction() {
    override fun update(e: AnActionEvent) {
        e.presentation.apply {
            val project = e.project?: return@apply
            isVisible = project.isUnityProject.valueOrDefault
        }
    }
}