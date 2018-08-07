package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.ActionGroup
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.actions.UnityPluginShowSettingsAction

/**
 * @author Kirill.Skrygan
 */
class UnityImportantActionsGroup : ActionGroup() {

    override fun getChildren(e: AnActionEvent?): Array<out AnAction> {
        return arrayOf(SwitchUIMode(), UnityPluginShowSettingsAction())
    }
}