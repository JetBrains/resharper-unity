package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rider.plugins.unity.actions.isUnityProject
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import java.awt.event.MouseEvent

class UnityImportantActions : DefaultActionGroup(), DumbAware {
    override fun actionPerformed(e: AnActionEvent) {
        val popup = JBPopupFactory.getInstance().createActionGroupPopup("", UnityImportantActionsGroup(), e.dataContext, JBPopupFactory.ActionSelectionAid.MNEMONICS, true)
        var point = JBPopupFactory.getInstance().guessBestPopupLocation(e.dataContext)
        if (e.inputEvent is MouseEvent) {
            point = RelativePoint(e.inputEvent as MouseEvent)
        }

        popup.show(point)
    }

    override fun update(e: AnActionEvent) {
        if (!e.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true
        e.presentation.icon = UnityIcons.Actions.ImportantActions
    }
}