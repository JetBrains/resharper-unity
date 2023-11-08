package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.util.application
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

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
        withContext(Dispatchers.EDT) {
            if (project.isDisposed) return@withContext
            val unityUIManager = UnityUIManager.getInstance(project)
            if (UnityProjectDiscoverer.getInstance(project).isUnityProject) {
                if (unityUIManager.hasMinimizedUi.value == null || unityUIManager.hasMinimizedUi.hasTrueValue())
                    ensureMinimizedUI(project)
            }
        }
    }
}