package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs
import com.jetbrains.rider.projectView.solution


open class StartUnityAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        startUnity(project)
    }

    override fun update(e: AnActionEvent) {
        val model = e.project?.solution?.frontendBackendModel
        val version = model?.unityApplicationData?.valueOrNull?.applicationVersion

        if (version != null)
            e.presentation.text = "Start Unity ($version)"

        e.presentation.isEnabled = version != null && e.project.isConnectedToEditor()
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