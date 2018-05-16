package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowManager
import com.jetbrains.rider.build.actions.ActiveConfigurationAndPlatformAction
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.lifetime
import com.jetbrains.rider.util.idea.tryGetComponent
import com.jetbrains.rider.util.reactive.whenTrue

class UnityUIMinimizer : StartupActivity {
    companion object {

        fun ensureMinimizedUI(project: Project) {
            application.assertIsDispatchThread()
            if(project.isDisposed)
                return

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                val toolWindowManager = ToolWindowManager.getInstance(project)

                toolWindowManager.getToolWindow("NuGet") ?: return@doWhenFocusSettlesDown
                toolWindowManager.unregisterToolWindow("NuGet")

                project.solution.rdUnityModel.hideSolutionConfiguration.advise(project.lifetime) {
                    if (it)
                        ActiveConfigurationAndPlatformAction.hiddenForProjects.add(project)
                    else
                        ActiveConfigurationAndPlatformAction.hiddenForProjects.remove(project)
                }
            }
        }
    }

    override fun runActivity(project: Project) {
        val unityUiManager = project.tryGetComponent<UnityUIManager>() ?: return

        unityUiManager.isUnityUI.whenTrue(project.lifetime, {
            application.invokeLater {
                ensureMinimizedUI(project)
            }
        })
    }
}