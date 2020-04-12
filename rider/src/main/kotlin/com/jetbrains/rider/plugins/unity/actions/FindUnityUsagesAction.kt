package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.actions.base.RiderAnAction

class FindUnityUsagesAction : RiderAnAction("FindUnityUsages") {
    override fun update(e: AnActionEvent) {
        e.presentation.isEnabledAndVisible = e.isUnityProject()
        super.update(e)
    }
}