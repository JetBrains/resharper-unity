package com.jetbrains.rider.plugins.unity

import com.intellij.ide.actions.SaveAllAction
import com.intellij.ide.actions.SaveDocumentAction
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.ex.ActionManagerEx
import com.intellij.openapi.actionSystem.ex.AnActionListener
import com.intellij.openapi.project.Project
import com.jetbrains.rider.util.idea.ILifetimedComponent
import com.jetbrains.rider.util.idea.LifetimedComponent

class SaveAllTracker(val project: Project, val actionManagerEx: ActionManagerEx) : ILifetimedComponent by LifetimedComponent(project) {

    init {
        actionManagerEx.addAnActionListener(FileListenerImpl(project))
    }

    class FileListenerImpl(val project: Project) : AnActionListener {
        override fun beforeActionPerformed(action: AnAction?, dataContext: DataContext?, event: AnActionEvent?) {

        }

        override fun afterActionPerformed(action: AnAction, dataContext: DataContext, event: AnActionEvent) {
            super.afterActionPerformed(action, dataContext, event)

            if ((action is SaveAllAction || action is SaveDocumentAction)) {
                ProjectCustomDataHost.CallBackendRefresh(project)
            }
        }
    }
}