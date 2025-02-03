package com.jetbrains.rider.plugins.unity

import com.intellij.ide.actions.SaveAllAction
import com.intellij.ide.actions.SaveDocumentAction
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.AnActionResult
import com.intellij.openapi.actionSystem.ex.AnActionListener
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class SaveAllActionListener : AnActionListener {
    override fun afterActionPerformed(action: AnAction, event: AnActionEvent, result: AnActionResult) {
        if (action !is SaveAllAction && action !is SaveDocumentAction) return
        val project = event.project ?: return

        project.solution.frontendBackendModel.refresh.fire(false)
        super.afterActionPerformed(action, event, result)
    }
}