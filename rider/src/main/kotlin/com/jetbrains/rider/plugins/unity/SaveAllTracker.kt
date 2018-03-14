package com.jetbrains.rider.plugins.unity

import com.intellij.ide.actions.SaveAllAction
import com.intellij.ide.actions.SaveDocumentAction
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.ex.ActionManagerEx
import com.intellij.openapi.actionSystem.ex.AnActionListener
import com.intellij.openapi.project.Project
import com.jetbrains.rider.util.idea.LifetimedProjectComponent

class SaveAllTracker(project: Project, val actionManagerEx: ActionManagerEx) : LifetimedProjectComponent(project) {
    init {
        val listener = FileListenerImpl(project)
        actionManagerEx.addAnActionListener(listener)
        componentLifetime.add { actionManagerEx.removeAnActionListener(listener) }
    }

    class FileListenerImpl(val project: Project) : AnActionListener {
        override fun beforeActionPerformed(action: AnAction?, dataContext: DataContext?, event: AnActionEvent?) {

        }

        override fun afterActionPerformed(action: AnAction?, dataContext: DataContext?, event: AnActionEvent?) {
            super.afterActionPerformed(action, dataContext, event)

            if (action!=null && (action is SaveAllAction || action is SaveDocumentAction)) {
                UnityHost.CallBackendRefresh(project, false)
            }
        }
    }
}