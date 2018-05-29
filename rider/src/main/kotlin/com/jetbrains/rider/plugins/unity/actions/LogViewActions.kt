package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.solution
import java.io.File

class RiderUnityOpenEditorLogAction : RiderUnityLogViewAction("Open Unity Editor Log", "", UnityIcons.Unity.UnityEdit) {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        var f = project.solution.rdUnityModel.editorLogPath.valueOrNull
        if (f!=null)
        {
            val vf = VfsUtil.findFileByIoFile(File(f), true)
            if (vf!=null)
            {
                val descriptor = OpenFileDescriptor(project, vf)
                FileEditorManager.getInstance(project).openTextEditor(descriptor, true)
            }
        }
    }
}

class RiderUnityOpenPlayerLogAction : RiderUnityLogViewAction("Open Unity Player Log", "", UnityIcons.Unity.UnityEdit) {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        var f = project.solution.rdUnityModel.playerLogPath.valueOrNull
        if (f!=null)
        {
            val vf = VfsUtil.findFileByIoFile(File(f), true)
            if (vf!=null)
            {
                val descriptor = OpenFileDescriptor(project, vf)
                FileEditorManager.getInstance(project).openTextEditor(descriptor, true)
            }
        }
    }
}


class RiderUnityOpenEditorConsoleLogViewAction : RiderUnityLogViewAction("Open Unity Editor Log (via Unity)", "", UnityIcons.Unity.UnityEdit) {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.openEditorConsole.start(Unit)
    }
}

class RiderUnityOpenPlayerConsoleLogViewAction : RiderUnityLogViewAction("Open Unity Play Log (via Unity)", "", UnityIcons.Unity.UnityPlay) {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.openPlayerConsole.start(Unit)
    }
}