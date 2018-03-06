package com.jetbrains.rider.plugins.unity.ui

import com.intellij.ide.actions.ActivateToolWindowAction
import com.intellij.openapi.actionSystem.*
import com.jetbrains.rider.actions.RiderActions
import com.jetbrains.rider.plugins.unity.actions.UnityPluginShowSettingsAction
import com.jetbrains.rider.solutionAnalysis.actions.PauseSWEAAction
import com.jetbrains.rider.solutionAnalysis.actions.ToggleSWEAAction

/**
 * @author Kirill.Skrygan
 */
class UnityImportantActionsGroup : ActionGroup() {

    override fun getChildren(e: AnActionEvent?): Array<out AnAction> {
        return arrayOf(/*SwitchUIMode(), */UnityPluginShowSettingsAction())
    }
}