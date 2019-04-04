package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder

open class StartUnityAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        startUnity(project)
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
        fun startUnity(project: Project, vararg args: String): Process? {
            val appPath = UnityInstallationFinder.getInstance(project).getApplicationPath() ?: return null
            return startUnity(appPath, project, args)
        }

        fun startUnity(appPath: java.nio.file.Path, project: Project, args: Array<out String>): Process? {
            val path = appPath.toString()
            val projectPath = project.basePath.toString();
            val processBuilderArgs = mutableListOf(path, "-projectPath", projectPath)

            processBuilderArgs.addAll(args)

            val processBuilder = ProcessBuilder(processBuilderArgs)
            return processBuilder.start()
        }

        fun startUnityAndRider(project: Project) {
            startUnity(project, "-executeMethod", "JetBrains.Rider.Unity.Editor.RiderMenu.MenuOpenProject")
        }
    }
}