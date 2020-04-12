package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.isUnityGeneratedProject
import com.jetbrains.rd.util.reactive.Property

class SwitchUIMode : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val uiManager = UnityUIManager.getInstance(project)

        if(uiManager.hasMinimizedUi.hasTrueValue())
            UnityUIMinimizer.recoverFullUI(project)
        else
            UnityUIMinimizer.ensureMinimizedUI(project)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project == null) {
            e.presentation.isEnabled = false
            return
        }

        // Only enable UI switching for generated Unity projects. Sidecar projects
        // (class library in the main Unity folder) are fairly advanced anyway, so
        // leave things enabled. It also means these projects can access nuget
        if(!project.isUnityGeneratedProject()) {
            e.presentation.isEnabled = false
            return
        }

        if(UnityUIManager.getInstance(project).hasMinimizedUi.hasTrueValue())
            e.presentation.text = "Switch to Full UI"
        else
            e.presentation.text = "Switch to Minimized UI"

        super.update(e)
    }
}

fun Property<Boolean?>.hasTrueValue() : Boolean {
    return this.value != null && this.value!!
}