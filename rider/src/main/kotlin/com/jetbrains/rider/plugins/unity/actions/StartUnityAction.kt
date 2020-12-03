package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.util.getUnityArgs
import com.jetbrains.rider.plugins.unity.util.withDebugCodeOptimization
import com.jetbrains.rider.plugins.unity.util.withProjectPath
import com.jetbrains.rider.plugins.unity.util.withRiderPath
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

        e.presentation.isEnabled = version != null && !e.project.isConnectedToEditor()
        super.update(e)
    }

    companion object {
        fun startUnity(project: Project, vararg args: String): Process? {
            val processBuilderArgs = getUnityArgs(project).withProjectPath(project).withRiderPath()
            processBuilderArgs.addAll(args)
            return startUnity(processBuilderArgs)
        }

        fun startUnity(args: MutableList<String>): Process? {
            val processBuilder = ProcessBuilder(args)
            return processBuilder.start()
        }

        fun startUnityAndRider(project: Project) {
            startUnity(project, "-executeMethod", "JetBrains.Rider.Unity.Editor.RiderMenu.MenuOpenProject")
        }
    }
}