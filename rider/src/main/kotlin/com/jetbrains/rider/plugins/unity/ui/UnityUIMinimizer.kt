package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.startOnUiAsync
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.util.concurrency.annotations.RequiresEdt
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService

class UnityUIMinimizer : ProjectActivity {
    companion object {
        @RequiresEdt
        fun ensureMinimizedUI(project: Project) {
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

        @RequiresEdt
        fun recoverFullUI(project: Project) {
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
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        val unityUIManager = UnityUIManager.getInstance(project)

        UnityProjectDiscoverer.getInstance(project).isUnityProject.advise(lifetime) {
            lifetime.startOnUiAsync {
                if (it) {
                    if (project.isDisposed) return@startOnUiAsync
                    if (unityUIManager.hasMinimizedUi.value == null || unityUIManager.hasMinimizedUi.hasTrueValue())
                        ensureMinimizedUI(project)
                }
                else {
                    if (project.isDisposed) return@startOnUiAsync
                    recoverFullUI(project)
                }
            }
        }
    }
}