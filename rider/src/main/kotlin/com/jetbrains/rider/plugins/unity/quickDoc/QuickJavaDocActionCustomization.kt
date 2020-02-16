package com.jetbrains.rider.plugins.unity.quickDoc

import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.Presentation
import com.jetbrains.rdclient.actions.base.BackendDelegatingAction
import com.jetbrains.rdclient.actions.base.BackendDelegatingActionCustomization
import com.jetbrains.rdclient.services.IdeBackend

class QuickJavaDocActionCustomization: BackendDelegatingActionCustomization() {
    // Called by BackendDelegatingAction#actionPerformed
    override fun executeBackend(e: AnActionEvent, frontendActionId: String, backend: IdeBackend) {
        val action = ActionManager.getInstance().getAction(frontendActionId) as? BackendDelegatingAction
        action?.frontendAction?.let {
            it.actionPerformed(e)
            return
        }
        super.executeBackend(e, frontendActionId, backend)
    }

    // Called by BackendDelegatingAction#beforeActionPerformedUpdate. Return value is "enabled"
    override fun frontendUpdate(frontendActionId: String, dataContext: DataContext, presentation: Presentation?, place: String): Boolean {
        val answer = super.frontendUpdate(frontendActionId, dataContext, presentation, place)
        return answer
    }
}