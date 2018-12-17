package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.microsoft.alm.helpers.Path


open class StartUnityAction : DumbAwareAction("Start Unity", "Start Unity with current project", UnityIcons.Actions.StartUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        StartUnity(project)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project != null) {
            val appVersion = UnityInstallationFinder.getInstance(project).getApplicationVersion()
            if (appVersion != null)
                e.presentation.text = "Start Unity ($appVersion)"
            else
                e.presentation.isEnabled = false
        }
        else
          e.presentation.isEnabled = false
        super.update(e)
    }

    companion object {
        fun StartUnity(project: Project, vararg args: String) {
            val appPath = UnityInstallationFinder.getInstance(project).getApplicationPath() ?: return
            var path = appPath.toString()
            if (SystemInfo.isMac)
                path = Path.combine(path, "Contents/MacOS/Unity")
            val processBuilderArgs = mutableListOf<String>(path, "-projectPath", project.basePath.toString())
            processBuilderArgs.addAll(args)

            ProcessBuilder(processBuilderArgs).start()
        }

        fun StartUnityAndRider(project: Project) {
            StartUnity(project, "-executeMethod", "JetBrains.Rider.Unity.Editor.RiderMenu.MenuOpenProject")
        }
    }
}