package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager
import com.jetbrains.rider.build.actions.ActiveConfigurationAndPlatformAction
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.lifetime
import com.jetbrains.rider.util.idea.tryGetComponent
import com.jetbrains.rider.util.reactive.whenTrue

class UnityUIMinimizer : StartupActivity {
    companion object {
        val minimizedUIs = hashSetOf<Project>()

        fun ensureMinimizedUI(project: Project) {
            application.assertIsDispatchThread()
            if(project.isDisposed)
                return

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                try {
                    val toolWindowManager = ToolWindowManager.getInstance(project)

                    toolWindowManager.getToolWindow("NuGet") ?: return@doWhenFocusSettlesDown
                    toolWindowManager.unregisterToolWindow("NuGet")
                    toolWindowManager.unregisterToolWindow("Database")

                    ActiveConfigurationAndPlatformAction.hiddenForProjects.add(project)
                } finally {
                    minimizedUIs.add(project)
                }
            }

        }

        fun recoverFullUI(project: Project) {
            application.assertIsDispatchThread()
            if(project.isDisposed)
                return

            IdeFocusManager.getInstance(project).doWhenFocusSettlesDown {
                try {
                    val toolWindowManager = ToolWindowManager.getInstance(project)
                    toolWindowManager.registerToolWindow("NuGet", true, ToolWindowAnchor.BOTTOM)
                    toolWindowManager.registerToolWindow("Database", true, ToolWindowAnchor.RIGHT)

                    ActiveConfigurationAndPlatformAction.hiddenForProjects.remove(project)

                } finally {
                    minimizedUIs.remove(project)
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