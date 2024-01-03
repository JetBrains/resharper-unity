package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.DumbAwareAction

class UnityPluginShowSettingsAction : DumbAwareAction() {
    companion object {
        const val actionId = "ShowUnitySettingsInRider"
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        ShowSettingsUtil.getInstance().showSettingsDialog(project, UnityPluginActionsBundle.message("configurable.name.unity.engine"))
    }
}
