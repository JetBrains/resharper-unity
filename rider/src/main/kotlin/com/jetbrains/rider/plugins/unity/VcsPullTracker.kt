package com.jetbrains.rider.plugins.unity

import com.intellij.application.subscribe
import com.intellij.openapi.Disposable
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.ex.AnActionListener
import com.intellij.openapi.components.ProjectComponent
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.update.AbstractCommonUpdateAction
import com.jetbrains.rider.model.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class VcsPullTracker(project: Project) : ProjectComponent, Disposable {
    init {
        AnActionListener.TOPIC.subscribe(this, ListenerImpl(project))
    }

    override fun dispose() {
    }

    class ListenerImpl(val project: Project) : AnActionListener {
        override fun afterActionPerformed(action: AnAction, dataContext: DataContext, event: AnActionEvent) {
            super.afterActionPerformed(action, dataContext, event)

            if (action is AbstractCommonUpdateAction) {
                project.solution.frontendBackendModel.refresh.fire(false)
            }
        }
    }
}