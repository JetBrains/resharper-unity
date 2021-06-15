package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.run.UnityProcessPickerDialog

class AttachToUnityProcessAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        val dialog = UnityProcessPickerDialog(project)
        dialog.show()
    }
}