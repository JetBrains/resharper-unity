package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.util.application
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer

class UnityUIMinimizer : ProjectActivity {
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
            }
        }
    }

    override suspend fun execute(project: Project) {
        application.invokeLater {
            val unityUIManager = UnityUIManager.getInstance(project)
            // Only hide UI for generated projects, so that sidecar projects can still access nuget
            if (UnityProjectDiscoverer.getInstance(project).isUnityGeneratedProject) {
                if (unityUIManager.hasMinimizedUi.value == null || unityUIManager.hasMinimizedUi.hasTrueValue())
                    ensureMinimizedUI(project)
            }
        }
    }
}