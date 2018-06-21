package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.util.idea.tryGetComponent
import com.jetbrains.rider.util.reactive.Property

class SwitchUIMode : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val uiManager = project.tryGetComponent<UnityUIManager>() ?: return

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

        val uiManager = project.tryGetComponent<UnityUIManager>()
        if (uiManager == null) {
            e.presentation.isEnabled = false
            return
        }

        val unityReferenceDiscoverer = project.tryGetComponent<UnityReferenceDiscoverer>()
        if (unityReferenceDiscoverer == null) {
            e.presentation.isEnabled = false
            return
        }

        if(!unityReferenceDiscoverer.isUnityGeneratedProject) {
            e.presentation.isEnabled = false
            return
        }

        if(uiManager.hasMinimizedUi.hasTrueValue()){
            e.presentation.text = "Switch to Full UI"}
        else
            e.presentation.text = "Switch to Minimized UI"

        super.update(e)
    }
}

fun Property<Boolean?>.hasTrueValue() : Boolean {
    return this.value != null && this.value!!
}