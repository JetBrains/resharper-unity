package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.plugins.unity.util.attach.UnityProcessPickerDialog

class AttachToUnityProcessAction : AnAction(UnityIcons.Icons.AttachEditorDebugConfiguration) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        val dialog = UnityProcessPickerDialog(project)
        dialog.show()
    }
}