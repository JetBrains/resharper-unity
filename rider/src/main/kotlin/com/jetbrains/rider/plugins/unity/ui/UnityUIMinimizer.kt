package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowManager
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.build.actions.ActiveConfigurationAndPlatformAction

class UnityUIMinimizer : StartupActivity {
    companion object {
        fun ensureMinimizedUI(project: Project) {
            application.assertIsDispatchThread()
            if (project.isDisposed)
                return

            val unityUiManager = UnityUIManager.getInstance(project)
            unityUiManager.hasMinimizedUi.value = true

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                val toolWindowManager = ToolWindowManager.getInstance(project)

                val nuget = toolWindowManager.getToolWindow("NuGet")
                    ?: return@doWhenFocusSettlesDown
                nuget.isShowStripeButton = false

                ActiveConfigurationAndPlatformAction.hiddenForProjects.add(project)
            }
        }

        fun recoverFullUI(project: Project) {
            application.assertIsDispatchThread()
            if (project.isDisposed)
                return

            val unityUiManager = UnityUIManager.getInstance(project)
            unityUiManager.hasMinimizedUi.value = false

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                val toolWindowManager = ToolWindowManager.getInstance(project)
                val toolWindow = toolWindowManager.getToolWindow("NuGet")
                    ?: return@doWhenFocusSettlesDown
                toolWindow.isShowStripeButton = true

                ActiveConfigurationAndPlatformAction.hiddenForProjects.remove(project)
            }
        }
    }

    override fun runActivity(project: Project) {
        application.invokeLater {
            val unityUIManager = UnityUIManager.getInstance(project)
            if (unityUIManager.hasMinimizedUi.hasTrueValue()) {
                ensureMinimizedUI(project)
            }
        }
    }
}