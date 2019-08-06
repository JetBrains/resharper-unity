package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs


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
            val processBuilderArgs = getUnityWithProjectArgs(project)
            processBuilderArgs.addAll(args)
            return startUnity(processBuilderArgs)
        }

        fun startUnity(args: MutableList<String>): Process? {
            val processBuilderArgs = mutableListOf<String>()
            processBuilderArgs.addAll(args)
            val processBuilder = ProcessBuilder(processBuilderArgs)
            return processBuilder.start()
        }

        fun startUnityAndRider(project: Project) {
            startUnity(project, "-executeMethod", "JetBrains.Rider.Unity.Editor.RiderMenu.MenuOpenProject")
        }
    }
}