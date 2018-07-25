package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.plugins.unity.run.attach.UnityProcessPickerDialog

class AttachToUnityProcessAction : AnAction("Attach to Unity Process…", "", UnityIcons.Actions.AttachToUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        val dialog = UnityProcessPickerDialog(project)
        dialog.show()
    }
}