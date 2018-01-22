package com.jetbrains.rider.plugins.unity.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.DumbAwareAction

class UnityPluginShowSettingsAction() : DumbAwareAction("Unity Plugin Settings...", "", AllIcons.General.Settings) {
    companion object {
        val instance get() = ActionManager.getInstance().getAction(UnityPluginShowSettingsAction::class.simpleName!!)!!
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        ShowSettingsUtil.getInstance().showSettingsDialog(project, "Unity Engine")
    }
}