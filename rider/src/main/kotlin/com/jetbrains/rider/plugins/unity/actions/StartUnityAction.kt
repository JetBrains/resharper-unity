package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.microsoft.alm.helpers.Path
import org.jetbrains.annotations.Nullable

open class StartUnityAction : DumbAwareAction("Start Unity", "Start Unity with current project.", UnityIcons.Actions.ImportantActions) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        StartUnity(project)
    }

    companion object {
         fun StartUnity(project: @Nullable Project) {
             val appPath = UnityInstallationFinder.getInstance(project).getApplicationPath()
             if (appPath == null) return

             var path = appPath.toString()
             if (SystemInfo.isMac)
                 path = Path.combine(path, "Contents/MacOS/Unity")

             ProcessBuilder(path, "-projectPath", project.basePath).start()
         }
    }
}