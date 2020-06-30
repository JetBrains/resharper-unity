package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rider.plugins.unity.run.UnityProcessPickerDialog
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil

class AttachToUnityProcessAction : AnAction(), DumbAware {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        val dialog = UnityProcessPickerDialog(project)
        dialog.onOk.advise(project.lifetime){
            UnityRunUtil.attachToUnityProcess(it.host, it.debuggerPort, it.id, project, it.isEditor)
        }
        dialog.show()
    }
}