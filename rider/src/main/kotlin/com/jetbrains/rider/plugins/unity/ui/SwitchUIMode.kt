package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAware

class SwitchUIMode : AnAction(), DumbAware {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        if(UnityUIMinimizer.minimizedUIs.contains(project))
            UnityUIMinimizer.recoverFullUI(project)
        else
            UnityUIMinimizer.ensureMinimizedUI(project)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return


        if(UnityUIMinimizer.minimizedUIs.contains(project))
            e.presentation.text = "Switch to Full UI"
        else
            e.presentation.text = "Switch to UI minimized for Unity"

        super.update(e)
    }
}

