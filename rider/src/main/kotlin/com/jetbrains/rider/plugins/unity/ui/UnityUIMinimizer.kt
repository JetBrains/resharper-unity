package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.impl.ToolWindowImpl
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.build.actions.ActiveConfigurationAndPlatformAction
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.tryGetComponent

class UnityUIMinimizer : StartupActivity {
    companion object {
        fun ensureMinimizedUI(project: Project) {
            application.assertIsDispatchThread()
            if (project.isDisposed)
                return

            val unityUiManager = project.tryGetComponent<UnityUIManager>() ?: return
            unityUiManager.hasMinimizedUi.value = true

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                val toolWindowManager = ToolWindowManager.getInstance(project)

                val nuget = toolWindowManager.getToolWindow("NuGet") as? ToolWindowImpl
                    ?: return@doWhenFocusSettlesDown
                nuget.removeStripeButton()

                ActiveConfigurationAndPlatformAction.hiddenForProjects.add(project)
            }
        }

        fun recoverFullUI(project: Project) {
            application.assertIsDispatchThread()
            if (project.isDisposed)
                return

            val unityUiManager = project.tryGetComponent<UnityUIManager>() ?: return
            unityUiManager.hasMinimizedUi.value = false

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                val toolWindowManager = ToolWindowManager.getInstance(project)
                val toolWindow = toolWindowManager.getToolWindow("NuGet") as? ToolWindowImpl
                    ?: return@doWhenFocusSettlesDown
                toolWindow.showStripeButton()

                ActiveConfigurationAndPlatformAction.hiddenForProjects.remove(project)
            }
        }
    }

    override fun runActivity(project: Project) {
        val unityUiManager = project.tryGetComponent<UnityUIManager>() ?: return
        val unityReferenceDiscoverer = project.tryGetComponent<UnityReferenceDiscoverer>() ?: return

        if(unityUiManager.hasMinimizedUi.hasTrueValue() && unityReferenceDiscoverer.isUnityGeneratedProject) {
            application.invokeLater {
                ensureMinimizedUI(project)
            }
        }
    }
}


