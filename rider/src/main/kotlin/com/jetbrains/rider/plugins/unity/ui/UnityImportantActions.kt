package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import java.awt.event.MouseEvent

class UnityImportantActions : AnAction(null, "Unity Related Actions", UnityIcons.Logo) {
    override fun actionPerformed(e: AnActionEvent) {
        val popup = JBPopupFactory.getInstance().createActionGroupPopup("", UnityImportantActionsGroup(), e.dataContext, JBPopupFactory.ActionSelectionAid.MNEMONICS, true)
        var point = JBPopupFactory.getInstance().guessBestPopupLocation(e.dataContext)
        if (e.inputEvent is MouseEvent) {
            point = RelativePoint(e.inputEvent as MouseEvent)
        }

        popup.show(point)
    }
}