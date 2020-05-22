package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.EditorState
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs
import com.jetbrains.rider.projectView.solution


open class StartUnityAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        startUnity(project)
    }

    override fun update(e: AnActionEvent) {
        val model = e.project?.solution?.rdUnityModel
        val version = model?.unityApplicationData?.valueOrNull?.applicationVersion
        val state = model?.editorState?.valueOrNull

        if (version != null)
            e.presentation.text = "Start Unity ($version)"

        e.presentation.isEnabled = state == EditorState.Disconnected
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