package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.ex.AnActionListener
import com.intellij.openapi.vcs.update.AbstractCommonUpdateAction
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.Project

class VcsPullTracker : AnActionListener {
    override fun afterActionPerformed(action: AnAction, dataContext: DataContext, event: AnActionEvent) {
        if (action !is AbstractCommonUpdateAction) return
        val project = dataContext.Project ?: return
        project.solution.rdUnityModel.refresh.fire(false)
    }
}